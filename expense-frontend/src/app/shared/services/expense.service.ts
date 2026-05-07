import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Expense, CreateExpenseRequest, ExpenseListResponse, MonthlySummary } from '../models/expense.model';

/**
 * Service for expense CRUD operations.
 */
@Injectable({
  providedIn: 'root'
})
export class ExpenseService {
  private apiUrl = '/api/expenses';

  constructor(private http: HttpClient) { }

  /**
   * Create a new expense.
   */
  create(request: CreateExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(this.apiUrl, request);
  }

  /**
   * Get expenses for a specific month.
   */
  getByMonth(month: number, year: number): Observable<ExpenseListResponse> {
    let params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    return this.http.get<ExpenseListResponse>(this.apiUrl, { params });
  }

  /**
   * Get single expense by ID.
   */
  getById(id: string): Observable<Expense> {
    return this.http.get<Expense>(`${this.apiUrl}/${id}`);
  }

  /**
   * Update an expense.
   */
  update(id: string, request: CreateExpenseRequest): Observable<Expense> {
    return this.http.put<Expense>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete an expense.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get monthly summary for reporting.
   */
  getMonthlySummary(): Observable<MonthlySummary[]> {
    return this.http.get<MonthlySummary[]>(`${this.apiUrl}/report/monthly`);
  }

  /**
   * Export expenses to file.
   */
  export(format: 'csv' | 'xlsx'): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export?format=${format}`, { responseType: 'blob' });
  }
}
