import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AppError, ErrorResponse } from '../models/error.model';

/**
 * Centralized error state management.
 */
@Injectable({
  providedIn: 'root'
})
export class ErrorService {
  private errorSubject = new BehaviorSubject<AppError | null>(null);
  public error$ = this.errorSubject.asObservable();

  /**
   * Emit error to be displayed in UI.
   */
  setError(error: AppError): void {
    this.errorSubject.next(error);
  }

  /**
   * Get current error.
   */
  getError(): AppError | null {
    return this.errorSubject.value;
  }

  /**
   * Clear current error.
   */
  clearError(): void {
    this.errorSubject.next(null);
  }

  /**
   * Transform HTTP error response to AppError.
   */
  handleHttpError(error: HttpErrorResponse | unknown): AppError {
    if (error instanceof HttpErrorResponse) {
      const errorResponse = (error.error as ErrorResponse) || {};
      return new AppError(
        errorResponse.message || 'An unexpected error occurred',
        errorResponse.logId,
        errorResponse.statusCode,
        errorResponse.details
      );
    }
    return new AppError('An unexpected error occurred', undefined, undefined, undefined);
  }
}
