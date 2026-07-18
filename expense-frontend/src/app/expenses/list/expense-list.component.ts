import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { ExpenseService } from '../../shared/services/expense.service';
import { ErrorService } from '../../shared/services/error.service';
import { Expense, CreateExpenseRequest } from '../../shared/models/expense.model';

type PaidFilter = 'both' | 'paid' | 'unpaid';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule
  ],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.css'
})
export class ExpenseListComponent implements OnInit, OnDestroy {
  expenses: Expense[] = [];
  isLoading = false;
  selectedMonth = new Date();
  paidFilter: PaidFilter = 'both';
  destroy$ = new Subject<void>();

  displayedColumns: string[] = ['date', 'category', 'description', 'amount', 'currency', 'actions', 'paid'];

  constructor(
    private expenseService: ExpenseService,
    private errorService: ErrorService,
    private router: Router,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadExpenses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExpenses(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    const month = this.selectedMonth.getMonth() + 1;
    const year = this.selectedMonth.getFullYear();

    this.expenseService.getByMonth(month, year)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          setTimeout(() => {
            this.isLoading = false;
            this.expenses = response.items;
            this.cdr.markForCheck();
          }, 0);
        },
        error: (error) => {
          setTimeout(() => {
            this.isLoading = false;
            this.cdr.markForCheck();
            const appError = this.errorService.handleHttpError(error);
            this.errorService.setError(appError);
          }, 0);
        }
      });
  }

  onMonthChange(date: Date): void {
    this.selectedMonth = date;
    this.loadExpenses();
  }

  get filteredExpenses(): Expense[] {
    if (this.paidFilter === 'paid') return this.expenses.filter(e => e.paid);
    if (this.paidFilter === 'unpaid') return this.expenses.filter(e => !e.paid);
    return this.expenses;
  }

  get allPaid(): boolean {
    const visible = this.filteredExpenses;
    return visible.length > 0 && visible.every(e => e.paid);
  }

  get somePaid(): boolean {
    const visible = this.filteredExpenses;
    return visible.some(e => e.paid) && !this.allPaid;
  }

  togglePaid(expense: Expense, paid: boolean): void {
    const previous = expense.paid;
    expense.paid = paid;
    this.cdr.markForCheck();

    const request: CreateExpenseRequest = {
      description: expense.description,
      amount: expense.amount,
      currency: expense.currency,
      category: expense.category,
      purchaseDate: expense.purchaseDate,
      paid
    };

    this.expenseService.update(expense.id!, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        error: (error) => {
          expense.paid = previous;
          this.cdr.markForCheck();
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  toggleAll(paid: boolean): void {
    this.filteredExpenses.forEach(expense => this.togglePaid(expense, paid));
  }

  editExpense(expense: Expense): void {
    this.router.navigate(['/expenses', expense.id, 'edit']);
  }

  deleteExpense(expense: Expense): void {
    if (confirm(`Are you sure you want to delete this expense?`)) {
      this.expenseService.delete(expense.id!)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.expenses = this.expenses.filter(e => e.id !== expense.id);
          },
          error: (error) => {
            const appError = this.errorService.handleHttpError(error);
            this.errorService.setError(appError);
          }
        });
    }
  }

  createNewExpense(): void {
    this.router.navigate(['/expenses/new']);
  }

  get totalAmount(): number {
    return this.filteredExpenses.reduce((sum, expense) => sum + expense.amount, 0);
  }

  formatDate(date: string | Date | null | undefined): string {
    if (!date) return '';
    const d = new Date(date);
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
