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
import { ExpenseService } from '../../shared/services/expense.service';
import { ErrorService } from '../../shared/services/error.service';

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
  template: `
    <div class="reports-container">
      <mat-card class="header-card">
        <mat-card-header>
          <mat-card-title>Expense Reports</mat-card-title>
          <mat-card-subtitle>View your monthly and category-wise expense summaries</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="controls-row">
            <mat-form-field appearance="outline">
              <mat-label>Select Month</mat-label>
              <input matInput [matDatepicker]="picker" 
                     [value]="selectedMonth" 
                     (dateChange)="onMonthChange($event.value)"
                     readonly>
              <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-datepicker #picker startView="multi-year"></mat-datepicker>
            </mat-form-field>
          </div>
        </mat-card-content>
      </mat-card>

      @if (isLoading) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
        </div>
      }

      @if (!isLoading && summary) {
        <mat-card class="summary-card">
          <mat-card-header>
            <mat-card-title>Monthly Summary</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="summary-row">
              <div class="summary-item">
                <span class="label">Total Expenses</span>
                <span class="value">{{ summary.totalAmount.toFixed(2) }}</span>
              </div>
              <div class="summary-item">
                <span class="label">Number of Transactions</span>
                <span class="value">{{ summary.count }}</span>
              </div>
              <div class="summary-item">
                <span class="label">Average Amount</span>
                <span class="value">{{ getAverageAmount().toFixed(2) }}</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        @if (categoryBreakdown && categoryBreakdown.length > 0) {
          <mat-card class="breakdown-card">
            <mat-card-header>
              <mat-card-title>Category Breakdown</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="category-list">
                @for (category of categoryBreakdown; track category.name) {
                  <div class="category-item">
                    <span class="category-name">{{ category.name }}</span>
                    <span class="category-amount">{{ category.totalAmount.toFixed(2) }}</span>
                    <div class="category-bar">
                      <div class="progress-bar" [style.width.%]="getPercentage(category.totalAmount)"></div>
                    </div>
                  </div>
                }
              </div>
            </mat-card-content>
          </mat-card>
        }
      }

      @if (!isLoading && !summary) {
        <mat-card class="empty-state">
          <mat-card-content>
            <mat-icon class="empty-icon">assessment</mat-icon>
            <p>No expenses found for this period</p>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .reports-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header-card {
      margin-bottom: 20px;
    }

    .controls-row {
      display: flex;
      gap: 20px;
      align-items: center;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 40px;
    }

    .summary-card {
      margin-bottom: 20px;
    }

    .summary-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 20px;
      padding: 10px;
    }

    .summary-item {
      display: flex;
      flex-direction: column;
      gap: 10px;
      padding: 15px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }

    .summary-item .label {
      font-size: 12px;
      color: #999;
      text-transform: uppercase;
    }

    .summary-item .value {
      font-size: 24px;
      font-weight: 500;
      color: #333;
    }

    .breakdown-card {
      margin-bottom: 20px;
    }

    .category-list {
      display: flex;
      flex-direction: column;
      gap: 15px;
    }

    .category-item {
      display: flex;
      align-items: center;
      gap: 15px;
    }

    .category-name {
      min-width: 100px;
      font-weight: 500;
    }

    .category-amount {
      min-width: 80px;
      text-align: right;
      font-weight: 500;
    }

    .category-bar {
      flex: 1;
      height: 20px;
      background-color: #e0e0e0;
      border-radius: 10px;
      overflow: hidden;
    }

    .progress-bar {
      height: 100%;
      background-color: #1976d2;
      border-radius: 10px;
    }

    .empty-state {
      text-align: center;
      padding: 40px;
    }

    .empty-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: #999;
      margin-bottom: 20px;
    }

    .empty-state p {
      color: #999;
      font-size: 14px;
    }
  `]
})
export class ReportViewComponent implements OnInit, OnDestroy {
  isLoading = false;
  selectedMonth = new Date();
  summary: any = null;
  categoryBreakdown: any[] = [];
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
        next: (data) => {
          this.isLoading = false;
          this.summary = data;
          this.categoryBreakdown = this.buildCategoryBreakdown(data);
        },
        error: (error) => {
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

  buildCategoryBreakdown(summary: any): any[] {
    // This would typically come from the API, but we'll parse it from expenses
    const breakdown: { [key: string]: number } = {};
    if (summary.items) {
      summary.items.forEach((expense: any) => {
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
