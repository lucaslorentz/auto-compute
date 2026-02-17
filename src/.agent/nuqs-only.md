# URL State Management Guidelines

To ensure stable, consistent, and readable URL parameters in this project, follow these rules:

1. **Strictly use `nuqs`**: All state that needs to persist in the URL must be managed via `nuqs` (e.g., `useQueryState`, `useQueryStates`).
2. **No Direct URL Manipulation**: Do **NOT** use `useSearchParams` or `navigate` from `react-router-dom` to manually update or read URL parameters.
3. **Atomic Updates**: Use `useQueryStates` (plural) to update multiple parameters at once. This prevents race conditions and ensures atomic URL updates.
4. **Custom Adapter**: Ensure all `nuqs` usage is wrapped in the provided `HashRouterNuqsAdapter`. This adapter is specifically configured to:
   - Handle hash-based routing.
   - Prevent unnecessary percent-encoding of parameters (keeping JSON and lists readable in the URL).
5. **Clear State with `null`**: To remove a parameter from the URL, set its value to `null`. Do not use empty strings of other falsy values that might leave trailing equal signs (e.g., `?sortBy=`).

By following these rules, we avoid duplicate parameters, race conditions, and unreadable "percent-encoded" clutter in the browser address bar.
