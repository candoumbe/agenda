/**
 * Represents a paginated response from the API.
 */
export interface PageOf<T> {
  /** Current page number (1-indexed) */
  page: number;

  /** Total number of pages */
  total: number;

  /** Number of items in the current page */
  count: number;

  /** Requested page size (when provided by backend) */
  pageSize?: number;

  /** Total number of matching items across all pages (when provided by backend) */
  totalCount?: number;

  /** Items in the current page */
  items: T[];

  /** Navigation links */
  links: PageLinks;
}

/**
 * Represents pagination links for navigation
 */
export interface PageLinks {
  /** Link to the first page */
  first?: PageLink;

  /** Link to the last page */
  last?: PageLink;

  /** Link to the previous page */
  previous?: PageLink;

  /** Link to the next page */
  next?: PageLink;
}

/**
 * Represents a single pagination link
 */
export interface PageLink {
  href: string;
  relations: string[];
}
