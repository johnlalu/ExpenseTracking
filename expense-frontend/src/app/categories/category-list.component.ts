import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CategoryService } from '../shared/services/category.service';
import { ErrorService } from '../shared/services/error.service';
import { Category } from '../shared/models/category.model';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.css'
})
export class CategoryListComponent implements OnInit, OnDestroy {
  categories: Category[] = [];
  categoryForm: FormGroup;
  isLoading = false;
  isSubmitting = false;
  destroy$ = new Subject<void>();
  displayedColumns: string[] = ['name', 'type', 'actions'];

  constructor(
    private categoryService: CategoryService,
    private errorService: ErrorService,
    private formBuilder: FormBuilder
  ) {
    this.categoryForm = this.formBuilder.group({
      name: ['', [Validators.required, Validators.minLength(2)]]
    });
  }

  ngOnInit(): void {
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.isLoading = true;
    this.categoryService.getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (categories: Category[]) => {
          this.isLoading = false;
          this.categories = categories;
        },
        error: (error: any) => {
          this.isLoading = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  onAddCategory(): void {
    if (this.categoryForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    const { name } = this.categoryForm.value;

    this.categoryService.create(name)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newCategory: Category) => {
          this.isSubmitting = false;
          this.categories.push(newCategory);
          this.categoryForm.reset();
        },
        error: (error: any) => {
          this.isSubmitting = false;
          const appError = this.errorService.handleHttpError(error);
          this.errorService.setError(appError);
        }
      });
  }

  onDeleteCategory(category: Category): void {
    if (confirm(`Are you sure you want to delete the category "${category.name}"?`)) {
      this.categoryService.delete(category.id!)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.categories = this.categories.filter(c => c.id !== category.id);
          },
          error: (error: any) => {
            const appError = this.errorService.handleHttpError(error);
            this.errorService.setError(appError);
          }
        });
    }
  }
}
