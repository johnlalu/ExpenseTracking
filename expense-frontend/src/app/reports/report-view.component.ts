import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ExpenseService } from '../shared/services/expense.service';
import { ErrorService } from '../shared/services/error.service';
import { MonthlySummary, CategoryBreakdown, Expense } from '../shared/models/expense.model';

@Component({
  selector: 'app-report-view',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './report-view.component.html',
  styleUrl: './report-view.component.css'
})
export class ReportViewComponent implements OnInit, OnDestroy {
  isLoading = false;
  selectedMonth = new Date();
  summary: MonthlySummary | null = null;
  categoryBreakdown: CategoryBreakdown[] = [];
  destroy$ = new Subject<void>();

  constructor(
    private expenseService: ExpenseService,
    private errorService: ErrorService
  ) {}

  ngOnInit(): void {
    this.loadReport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadReport(): void {
    this.isLoading = true;
    const month = this.selectedMonth.getMonth() + 1;
    const year = this.selectedMonth.getFullYear();

    this.expenseService.getMonthlySummary(month, year)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: MonthlySummary) => {
          this.isLoading = false;
          this.summary = data;
          this.categoryBreakdown = this.buildCategoryBreakdown(data);
        },
        error: (error: unknown) => {
          this.isLoading = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  onMonthChange(date: Date): void {
    this.selectedMonth = date;
    this.loadReport();
  }

  buildCategoryBreakdown(summary: MonthlySummary): CategoryBreakdown[] {
    // This would typically come from the API, but we'll parse it from expenses
    const breakdown: { [key: string]: number } = {};
    if (summary.items) {
      summary.items.forEach((expense: Expense) => {
        const category = expense.category || 'Other';
        breakdown[category] = (breakdown[category] || 0) + expense.amount;
      });
    }

    return Object.entries(breakdown)
      .map(([name, totalAmount]) => ({ name, totalAmount }))
      .sort((a, b) => b.totalAmount - a.totalAmount);
  }

  getAverageAmount(): number {
    if (!this.summary || this.summary.count === 0) return 0;
    return this.summary.totalAmount / this.summary.count;
  }

  getPercentage(amount: number): number {
    if (!this.summary || this.summary.totalAmount === 0) return 0;
    return (amount / this.summary.totalAmount) * 100;
  }
}
