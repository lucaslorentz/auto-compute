import { useEffect } from "react";
import { useLocation } from "react-router-dom";

export function useScrollToAnchor() {
  const { pathname, hash, key } = useLocation();

  useEffect(() => {
    if (hash === "") {
      window.scrollTo(0, 0);
      return;
    }

    const id = decodeURIComponent(hash.replace("#", ""));

    const tryScroll = () => {
      const element = document.getElementById(id);
      if (element) {
        element.scrollIntoView({
          block: "start",
          inline: "nearest",
          behavior: "smooth",
        });
        return true;
      }
      return false;
    };

    // Element may already be rendered (e.g. cached data)
    if (tryScroll()) return;

    // Otherwise wait for it to appear (data still loading)
    const observer = new MutationObserver(() => {
      if (tryScroll()) observer.disconnect();
    });

    observer.observe(document.body, { childList: true, subtree: true });

    // Safety timeout to avoid observing forever
    const timeout = setTimeout(() => observer.disconnect(), 10000);

    return () => {
      observer.disconnect();
      clearTimeout(timeout);
    };
  }, [pathname, hash, key]);
}
