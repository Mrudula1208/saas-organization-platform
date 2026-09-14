// Shape returned by every paginated list endpoint on the API:
// the rows for the current page plus the total count used for page controls.
export interface PagedResult<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
