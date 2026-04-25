/**
 * Search parameters for appointments list
 */
export interface SearchAppointmentsParams {
  /** Page number (1-indexed) */
  page?: number;

  /** Number of items per page */
  pageSize?: number;

  /** Filter by subject */
  subject?: string;

  /** Filter from date (ISO datetime) */
  from?: string;

  /** Filter to date (ISO datetime) */
  to?: string;

  /** Sort field */
  sort?: string;
}
