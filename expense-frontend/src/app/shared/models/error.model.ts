/**
 * Standardized error response.
 */
export interface ErrorResponse {
  message?: string;
  logId?: string;
  statusCode: number;
  details?: { [key: string]: string[] };
}

/**
 * Custom application error.
 */
export class AppError extends Error {
  constructor(
    override message: string,
    public logId?: string,
    public statusCode?: number,
    public details?: { [key: string]: string[] }
  ) {
    super(message);
  }
}
