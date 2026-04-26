# Skill: Frontend Pagination From API Metadata

## When to use

Use this pattern when a frontend list screen consumes a paginated API response that includes page metadata and navigation links.

## Goal

Keep displayed page numbers and next/previous behavior strictly aligned with backend pagination semantics.

## Recommended approach

1. Treat `response.page` as the source of truth for current page display.
2. Derive `totalPages` from `links.last` page query param when present.
3. Fallback to `response.total` only when `links.last` is absent.
4. Enable/disable previous/next actions from `links.previous` and `links.next` first, with numeric fallback (`currentPage` vs `totalPages`) only if links are absent.
5. Parse next/previous page targets from links when possible to avoid optimistic client-side drift.

## Search + pagination coupling

- On any filter change, reset requested page to 1 before calling the API.
- Send all active filters to API (do not filter only the loaded page client-side).

## Testing checklist

- Current page display mirrors `response.page`.
- Total pages follow `links.last` when `response.total` is inconsistent.
- Next/previous buttons honor link availability.
- Search criteria changes trigger page reset to 1.
- Request params include all supported filters.
