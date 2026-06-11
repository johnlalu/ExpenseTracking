import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
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
import { CategoryBreakdown, Expense } from '../shared/models/expense.model';

interface ReportSummary {
  totalAmount: number;
  count: number;
}

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
  summary: ReportSummary | null = null;
  categoryBreakdown: CategoryBreakdown[] = [];
  destroy$ = new Subject<void>();

  constructor(
    private expenseService: ExpenseService,
    private errorService: ErrorService,
    private cdr: ChangeDetectorRef
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

    this.expenseService.getByMonth(month, year)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.isLoading = false;
          const items = response.items || [];
          if (items.length > 0) {
            const totalAmount = items.reduce((sum, e) => sum + e.amount, 0);
            this.summary = { totalAmount, count: items.length };
            this.categoryBreakdown = this.buildCategoryBreakdown(items);
          } else {
            this.summary = null;
            this.categoryBreakdown = [];
          }
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.isLoading = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
          this.cdr.markForCheck();
        }
      });
  }

  onMonthChange(date: Date): void {
    this.selectedMonth = date;
    this.loadReport();
  }

  buildCategoryBreakdown(items: Expense[]): CategoryBreakdown[] {
    const breakdown: { [key: string]: number } = {};
    items.forEach((expense: Expense) => {
      const category = expense.category || 'Other';
      breakdown[category] = (breakdown[category] || 0) + expense.amount;
    });

    return Object.entries(breakdown)
      .map(([name, totalAmount]) => ({ name, totalAmount }))
      .sort((a, b) => b.totalAmount - a.totalAmount);
  }

  getAverageAmount(): number {
    if (!this.summary || (this.summary.count ?? 0) === 0) return 0;
    const totalAmount = this.summary.totalAmount ?? 0;
    const count = this.summary.count ?? 0;
    return totalAmount / count;
  }

  getPercentage(amount: number): number {
    if (!this.summary || (this.summary.totalAmount ?? 0) === 0) return 0;
    return (amount / (this.summary.totalAmount ?? 0)) * 100;
  }
}
