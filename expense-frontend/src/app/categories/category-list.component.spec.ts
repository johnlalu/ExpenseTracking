import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CategoryListComponent } from './category-list.component';
import { CategoryService } from '../shared/services/category.service';
import { ErrorService } from '../shared/services/error.service';
import { of, throwError } from 'rxjs';
import { Category } from '../shared/models/category.model';

describe('CategoryListComponent', () => {
  let component: CategoryListComponent;
  let fixture: ComponentFixture<CategoryListComponent>;
  let categoryService: { getAll: ReturnType<typeof vi.fn>; create: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let errorService: { handleHttpError: ReturnType<typeof vi.fn>; setError: ReturnType<typeof vi.fn> };

  const mockCategories: Category[] = [
    { id: '1', name: 'Groceries', isDefault: true },
    { id: '2', name: 'Entertainment', isDefault: false }
  ];

  beforeEach(async () => {
    categoryService = { getAll: vi.fn(), create: vi.fn(), delete: vi.fn() };
    errorService = { handleHttpError: vi.fn(), setError: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [CategoryListComponent, BrowserAnimationsModule],
      providers: [
        { provide: CategoryService, useValue: categoryService },
        { provide: ErrorService, useValue: errorService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    categoryService.getAll.mockReturnValue(of([]));
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load categories on init', () => {
      categoryService.getAll.mockReturnValue(of(mockCategories));

      component.ngOnInit();

      expect(categoryService.getAll).toHaveBeenCalled();
      expect(component.categories).toEqual(mockCategories);
      expect(component.isLoading).toBe(false);
    });

    it('should handle error when loading categories', () => {
      const error = { error: { message: 'Test error' } };
      categoryService.getAll.mockReturnValue(throwError(() => error));
      errorService.handleHttpError.mockReturnValue({ message: 'Test error' });

      component.ngOnInit();

      expect(errorService.handleHttpError).toHaveBeenCalledWith(error);
      expect(errorService.setError).toHaveBeenCalled();
    });
  });

  describe('onAddCategory', () => {
    it('should add a new category when form is valid', () => {
      const newCategory: Category = { id: '3', name: 'Dining', isDefault: false };
      categoryService.create.mockReturnValue(of(newCategory));
      component.categories = [...mockCategories];

      component.categoryForm.patchValue({ name: 'Dining' });
      component.onAddCategory();

      expect(categoryService.create).toHaveBeenCalledWith('Dining');
      expect(component.categories.length).toBe(3);
      expect(component.categoryForm.value.name).toBeNull();
    });

    it('should not add category when form is invalid', () => {
      component.categoryForm.patchValue({ name: '' });

      component.onAddCategory();

      expect(categoryService.create).not.toHaveBeenCalled();
    });

    it('should handle error when adding category', () => {
      const error = { error: { message: 'Test error' } };
      categoryService.create.mockReturnValue(throwError(() => error));
      errorService.handleHttpError.mockReturnValue({ message: 'Test error' });

      component.categoryForm.patchValue({ name: 'Dining' });
      component.onAddCategory();

      expect(errorService.handleHttpError).toHaveBeenCalledWith(error);
    });
  });

  describe('onDeleteCategory', () => {
    it('should delete a category when confirmed', () => {
      const categoryToDelete = mockCategories[1];
      component.categories = [...mockCategories];
      categoryService.delete.mockReturnValue(of(void 0));
      vi.spyOn(window, 'confirm').mockReturnValue(true);

      component.onDeleteCategory(categoryToDelete);

      expect(categoryService.delete).toHaveBeenCalledWith(categoryToDelete.id);
      expect(component.categories.length).toBe(1);
      expect(component.categories[0].id).toBe('1');
    });

    it('should not delete when user cancels confirmation', () => {
      const categoryToDelete = mockCategories[1];
      component.categories = [...mockCategories];
      vi.spyOn(window, 'confirm').mockReturnValue(false);

      component.onDeleteCategory(categoryToDelete);

      expect(categoryService.delete).not.toHaveBeenCalled();
      expect(component.categories.length).toBe(2);
    });
  });

  describe('Form validation', () => {
    it('should mark form as invalid when name is empty', () => {
      component.categoryForm.patchValue({ name: '' });
      expect(component.categoryForm.invalid).toBe(true);
    });

    it('should mark form as invalid when name is too short', () => {
      component.categoryForm.patchValue({ name: 'a' });
      expect(component.categoryForm.invalid).toBe(true);
    });

    it('should mark form as valid when name meets minimum length', () => {
      component.categoryForm.patchValue({ name: 'ab' });
      expect(component.categoryForm.valid).toBe(true);
    });
  });
});
