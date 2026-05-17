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
import { CategoryService } from '../../shared/services/category.service';
import { ErrorService } from '../../shared/services/error.service';
import { Category } from '../../shared/models/category.model';

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
  template: `
    <div class="categories-container">
      <mat-card class="header-card">
        <mat-card-header>
          <mat-card-title>Categories</mat-card-title>
          <mat-card-subtitle>Manage your expense categories</mat-card-subtitle>
        </mat-card-header>
      </mat-card>

      <div class="content-grid">
        <!-- Add Category Form -->
        <mat-card class="form-card">
          <mat-card-header>
            <mat-card-title>Add New Category</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="categoryForm" (ngSubmit)="onAddCategory()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Category Name</mat-label>
                <input matInput formControlName="name" placeholder="Enter category name" required>
                @if (categoryForm.get('name')?.invalid && categoryForm.get('name')?.touched) {
                  <mat-error>Category name is required</mat-error>
                }
              </mat-form-field>

              <button mat-raised-button color="primary" class="full-width" [disabled]="categoryForm.invalid || isSubmitting">
                @if (!isSubmitting) {
                  <mat-icon>add</mat-icon>
                }
                @if (isSubmitting) {
                  <mat-spinner diameter="20" class="button-spinner"></mat-spinner>
                }
                Add Category
              </button>
            </form>
          </mat-card-content>
        </mat-card>

        <!-- Categories List -->
        <mat-card class="list-card">
          <mat-card-header>
            <mat-card-title>Your Categories</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (isLoading) {
              <div class="loading-container">
                <mat-spinner></mat-spinner>
              </div>
            }

            @if (!isLoading && categories.length > 0) {
              <table mat-table [dataSource]="categories" class="categories-table">
                <!-- Name Column -->
                <ng-container matColumnDef="name">
                  <th mat-header-cell *matHeaderCellDef>Name</th>
                  <td mat-cell *matCellDef="let element">{{ element.name }}</td>
                </ng-container>

                <!-- Type Column -->
                <ng-container matColumnDef="type">
                  <th mat-header-cell *matHeaderCellDef>Type</th>
                  <td mat-cell *matCellDef="let element">
                    @if (element.isDefault) {
                      <span class="badge default-badge">Default</span>
                    }
                    @if (!element.isDefault) {
                      <span class="badge custom-badge">Custom</span>
                    }
                  </td>
                </ng-container>

                <!-- Actions Column -->
                <ng-container matColumnDef="actions">
                  <th mat-header-cell *matHeaderCellDef>Actions</th>
                  <td mat-cell *matCellDef="let element">
                    @if (!element.isDefault) {
                      <button mat-icon-button color="warn" (click)="onDeleteCategory(element)" title="Delete">
                        <mat-icon>delete</mat-icon>
                      </button>
                    }
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
              </table>
            }

            @if (!isLoading && categories.length === 0) {
              <div class="empty-state">
                <mat-icon class="empty-icon">folder_open</mat-icon>
                <p>No categories found</p>
              </div>
            }
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .categories-container {
      padding: 20px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header-card {
      margin-bottom: 20px;
    }

    .content-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
    }

    @media (max-width: 900px) {
      .content-grid {
        grid-template-columns: 1fr;
      }
    }

    .form-card,
    .list-card {
      min-height: 400px;
    }

    mat-form-field {
      width: 100%;
      margin-bottom: 15px;
    }

    .full-width {
      width: 100%;
    }

    .categories-table {
      width: 100%;
    }

    .badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
    }

    .default-badge {
      background-color: #e3f2fd;
      color: #1976d2;
    }

    .custom-badge {
      background-color: #f3e5f5;
      color: #7b1fa2;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 40px;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 40px 20px;
      color: #999;
    }

    .empty-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      margin-bottom: 20px;
      color: #ccc;
    }

    .empty-state p {
      font-size: 14px;
    }

    ::ng-deep .button-spinner {
      margin-right: 8px;
    }
  `]
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
        next: (categories) => {
          this.isLoading = false;
          this.categories = categories;
        },
        error: (error) => {
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

    this.categoryService.create({ name })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newCategory) => {
          this.isSubmitting = false;
          this.categories.push(newCategory);
          this.categoryForm.reset();
        },
        error: (error) => {
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
          error: (error) => {
            const appError = this.errorService.handleHttpError(error);
            this.errorService.setError(appError);
          }
        });
    }
  }
}
