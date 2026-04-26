# Skill: Backend Listing Pagination Contract

## Context

Use this pattern for paginated API listing endpoints consumed by UI pagination controls.

## Rules

1. Keep metadata semantics explicit:
   - `page`: current page index (1-based)
   - `pageSize`: requested page size
   - `count`: number of items in current page
   - `totalCount`: total number of matching items
   - `total`: total number of pages
2. Compute `total` as `ceil(totalCount / pageSize)` and guard empty result sets.
3. Build `previous` and `next` links from current page and total pages, never from current page item count.
4. Build `last` link from computed total pages (fallback to page 1 when there is no data).
5. When adding filters, carry all active criteria into pagination links so navigation preserves search context.

## Testing Checklist

- `total` reflects total pages, not total items.
- `totalCount` reflects matching items independently of page size.
- `next` is null on last page.
- `previous` is null on first page.
- Combined filters (e.g., subject + location + from/to) narrow results correctly.
