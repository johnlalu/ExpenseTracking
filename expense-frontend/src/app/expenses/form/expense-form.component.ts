import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { ExpenseService } from '../../shared/services/expense.service';
import { CategoryService } from '../../shared/services/category.service';
import { ErrorService } from '../../shared/services/error.service';
import { Expense, CreateExpenseRequest } from '../../shared/models/expense.model';
import { Category } from '../../shared/models/category.model';

/**
 * Component for creating and editing expenses.
 */
@Component({
  selector: 'app-expense-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatIconModule,
    MatCheckboxModule
  ],
  templateUrl: './expense-form.component.html',
  styleUrl: './expense-form.component.css'
})
export class ExpenseFormComponent implements OnInit, OnDestroy {
  expenseForm: FormGroup;
  isLoading = false;
  isSubmitting = false;
  categories: Category[] = [];
  expenseId: string | null = null;
  isEditMode = false;
  destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private expenseService: ExpenseService,
    private categoryService: CategoryService,
    private errorService: ErrorService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {
    this.expenseForm = this.createForm();
  }

  ngOnInit(): void {
    this.loadCategories();
    this.checkEditMode();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): FormGroup {
    return this.formBuilder.group({
      description: ['', Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      category: ['', Validators.required],
      purchaseDate: [new Date(), Validators.required],
      paid: [false]
    });
  }

  private checkEditMode(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['id']) {
        this.expenseId = params['id'];
        this.isEditMode = true;
        this.loadExpense();
      }
    });
  }

  private loadExpense(): void {
    if (!this.expenseId) return;

    this.isLoading = true;
    this.expenseService.getById(this.expenseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (expense) => {
          this.isLoading = false;
          this.populateForm(expense);
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.isLoading = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
          this.router.navigate(['/expenses']);
          this.cdr.markForCheck();
        }
      });
  }

  private loadCategories(): void {
    this.categoryService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
        },
        error: (error) => {
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  private populateForm(expense: Expense): void {
    this.expenseForm.patchValue({
      description: expense.description,
      amount: expense.amount,
      category: expense.category,
      purchaseDate: new Date(expense.purchaseDate!),
      paid: expense.paid ?? false
    });
  }

  onSubmit(): void {
    if (this.expenseForm.invalid) {
      this.markFormGroupTouched(this.expenseForm);
      return;
    }

    this.isSubmitting = true;
    const formValue = this.expenseForm.value;
    
    const request: CreateExpenseRequest = {
      description: formValue.description,
      amount: parseFloat(formValue.amount),
      category: formValue.category,
      purchaseDate: formValue.purchaseDate,
      paid: formValue.paid ?? false
    };

    const operation = this.isEditMode
      ? this.expenseService.update(this.expenseId!, request)
      : this.expenseService.create(request);

    operation.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/expenses']);
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isSubmitting = false;
        const appError = this.errorService.handleHttpError(error);
        this.errorService.setError(appError);
        this.cdr.markForCheck();
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/expenses']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  get descriptionError(): string {
    const control = this.expenseForm.get('description');
    if (control?.hasError('required') && control?.touched) {
      return 'Description is required';
    }
    return '';
  }

  get amountError(): string {
    const control = this.expenseForm.get('amount');
    if (control?.hasError('required') && control?.touched) {
      return 'Amount is required';
    }
    if (control?.hasError('min') && control?.touched) {
      return 'Amount must be greater than 0';
    }
    return '';
  }

  get categoryError(): string {
    const control = this.expenseForm.get('category');
    if (control?.hasError('required') && control?.touched) {
      return 'Category is required';
    }
    return '';
  }

  get dateError(): string {
    const control = this.expenseForm.get('purchaseDate');
    if (control?.hasError('required') && control?.touched) {
      return 'Purchase date is required';
    }
    return '';
  }
}
