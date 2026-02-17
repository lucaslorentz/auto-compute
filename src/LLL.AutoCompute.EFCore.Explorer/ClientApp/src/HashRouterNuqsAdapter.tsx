import { unstable_createAdapterProvider } from "nuqs/adapters/custom";
import { useNavigate } from "react-router-dom";
import { useCallback, useEffect, useState } from "react";

// ── Helpers ──────────────────────────────────────────────────────────────────

/** Return the "?key=val&…" portion that lives inside the hash fragment. */
function getHashSearch(): string {
  const hash = window.location.hash; // e.g. "#/entity/items?foo=bar"
  const q = hash.indexOf("?");
  return q >= 0 ? hash.slice(q) : "";
}

function hashSearchParams(): URLSearchParams {
  return new URLSearchParams(getHashSearch());
}

/** Given a full URL string (e.g. "/base/#/path?x=1"), pull out "?x=1". */
function searchFromUrl(url: string): string {
  const h = url.indexOf("#");
  if (h < 0) return "";
  const q = url.indexOf("?", h);
  return q >= 0 ? url.slice(q) : "";
}

// ── Micro-emitter ────────────────────────────────────────────────────────────

type Fn = (sp: URLSearchParams) => void;
const subs = new Set<Fn>();
function emit(sp: URLSearchParams) {
  subs.forEach((fn) => fn(sp));
}

// ── History patching ─────────────────────────────────────────────────────────
//
// React-router's navigate() ends up calling pushState / replaceState.
// We intercept those calls so that when *external* code (i.e. code that is NOT
// nuqs itself) changes the hash URL, the adapter's optimistic searchParams
// state stays in sync.  This is the mechanism that the official nuqs
// react-router adapter uses (via patchHistory + useOptimisticSearchParams),
// adapted here for HashRouter where search params live inside the hash.

let nuqsOwned = false;
let lastSearch = typeof location === "object" ? getHashSearch() : "";

if (typeof window !== "undefined" && !(history as any).__hashNuqs) {
  (history as any).__hashNuqs = true;

  const syncLast = () => { lastSearch = getHashSearch(); };
  window.addEventListener("popstate", syncLast);
  window.addEventListener("hashchange", syncLast);

  const _push = history.pushState.bind(history);
  const _replace = history.replaceState.bind(history);

  function maybeSync(url: string | URL | null | undefined) {
    if (nuqsOwned || !url) return;
    const s = searchFromUrl(String(url));
    if (s === lastSearch) return;
    lastSearch = s;
    // Synchronous emit so the React state update is batched with
    // react-router's own re-render (both triggered in the same pushState call).
    emit(new URLSearchParams(s));
  }

  history.pushState = function (state: any, title: string, url?: string | URL | null) {
    _push(state, title, url);
    maybeSync(url);
  };
  history.replaceState = function (state: any, title: string, url?: string | URL | null) {
    _replace(state, title, url);
    maybeSync(url);
  };
}

// ── Adapter provider ─────────────────────────────────────────────────────────

const HashRouterNuqsAdapter = unstable_createAdapterProvider((watchedKeys) => {
  const navigate = useNavigate();
  const nuqsKeys = new Set(watchedKeys ?? []);

  // Optimistic state kept in sync via emitter (nuqs writes) and
  // popstate / hashchange (browser back/forward, external navigations).
  const [searchParams, setSearchParams] = useState(hashSearchParams);

  useEffect(() => {
    const onEmit: Fn = (sp) => setSearchParams(new URLSearchParams(sp));
    const onPop = () => setSearchParams(hashSearchParams());

    subs.add(onEmit);
    window.addEventListener("popstate", onPop);
    window.addEventListener("hashchange", onPop);
    return () => {
      subs.delete(onEmit);
      window.removeEventListener("popstate", onPop);
      window.removeEventListener("hashchange", onPop);
    };
  }, []);

  const updateUrl = useCallback(
    (search: URLSearchParams, options: { history: string; scroll: boolean; shallow: boolean }) => {
      // 1 — Optimistic update.  Must NOT be wrapped in startTransition:
      //     navigate() triggers a regular-priority react-router re-render.
      //     If the emit is a transition (low-priority), React renders the
      //     route change first with stale searchParams → flickering.
      //     Keeping both as regular updates lets React batch them together.
      emit(search);

      // 2 — Build query-string without percent-encoding (preserves commas, parens, etc.)
      const parts: string[] = [];
      search.forEach((v, k) => parts.push(`${k}=${v}`));

      // Keep params that nuqs doesn't manage.
      const current = hashSearchParams();
      current.forEach((v, k) => {
        if (!nuqsKeys.has(k) && !search.has(k)) parts.push(`${k}=${v}`);
      });

      const qs = parts.length ? `?${parts.join("&")}` : "";

      // 3 — Navigate via react-router; the flag prevents our history patch
      //     from re-emitting (the optimistic emit in step 1 already covered it).
      nuqsOwned = true;
      navigate(
        { search: qs },
        { replace: options.history !== "push", preventScrollReset: !options.scroll },
      );
      nuqsOwned = false;
      lastSearch = getHashSearch();

      if (options.scroll) window.scrollTo(0, 0);
    },
    [navigate], // eslint-disable-line react-hooks/exhaustive-deps
  );

  // nuqs's throttle queue calls getSearchParamsSnapshot() at flush-time to
  // read the CURRENT search params (not a stale closure).  The built-in
  // fallback reads from `location.search`, which is always empty in HashRouter.
  // Providing our own snapshot that reads from the hash fixes the root cause.
  const getSearchParamsSnapshot = useCallback(() => hashSearchParams(), []);

  return { searchParams, updateUrl, getSearchParamsSnapshot };
});

export { HashRouterNuqsAdapter };
