import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ExpenseFormComponent } from './expense-form.component';
import { ExpenseService } from '../../shared/services/expense.service';
import { CategoryService } from '../../shared/services/category.service';
import { ErrorService } from '../../shared/services/error.service';
import { Expense } from '../../shared/models/expense.model';
import { Subject } from 'rxjs';

describe('ExpenseFormComponent', () => {
  let component: ExpenseFormComponent;
  let fixture: ComponentFixture<ExpenseFormComponent>;
  let expenseService: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; getById: ReturnType<typeof vi.fn> };
  let categoryService: { getAll: ReturnType<typeof vi.fn> };
  let errorService: { handleHttpError: ReturnType<typeof vi.fn>; setError: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let routeParams$: Subject<Record<string, string>>;

  const mockExpense: Expense = {
    id: '1',
    description: 'Team lunch',
    amount: 75.50,
    category: 'Meals',
    purchaseDate: new Date('2024-04-15'),
    paid: false
  };

  beforeEach(async () => {
    routeParams$ = new Subject();
    expenseService = { create: vi.fn(), update: vi.fn(), getById: vi.fn() };
    categoryService = { getAll: vi.fn().mockReturnValue(of([])) };
    errorService = { handleHttpError: vi.fn(), setError: vi.fn() };
    router = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ExpenseFormComponent, BrowserAnimationsModule],
      providers: [
        { provide: ExpenseService, useValue: expenseService },
        { provide: CategoryService, useValue: categoryService },
        { provide: ErrorService, useValue: errorService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { params: routeParams$.asObservable() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('form structure', () => {
    it('should not have a source control', () => {
      expect(component.expenseForm.get('source')).toBeNull();
    });

    it('should have required fields', () => {
      expect(component.expenseForm.get('description')).toBeTruthy();
      expect(component.expenseForm.get('amount')).toBeTruthy();
      expect(component.expenseForm.get('category')).toBeTruthy();
      expect(component.expenseForm.get('purchaseDate')).toBeTruthy();
    });

    it('should have paid control defaulting to false', () => {
      expect(component.expenseForm.get('paid')).toBeTruthy();
      expect(component.expenseForm.get('paid')!.value).toBe(false);
    });

    it('should be invalid when empty', () => {
      component.expenseForm.reset();
      expect(component.expenseForm.invalid).toBe(true);
    });

    it('should be valid when all required fields are filled', () => {
      component.expenseForm.patchValue({
        description: 'Lunch',
        amount: 20,
        category: 'Meals',
        purchaseDate: new Date()
      });
      expect(component.expenseForm.valid).toBe(true);
    });
  });

  describe('create mode', () => {
    it('should start in create mode when no route id', () => {
      expect(component.isEditMode).toBe(false);
    });

    it('should call create and navigate on valid submit', () => {
      expenseService.create.mockReturnValue(of(mockExpense));
      component.expenseForm.patchValue({
        description: 'Team lunch',
        amount: 75.50,
        category: 'Meals',
        purchaseDate: new Date()
      });

      component.onSubmit();

      expect(expenseService.create).toHaveBeenCalled();
      const requestArg = expenseService.create.mock.calls[0][0];
      expect(requestArg.source).toBeUndefined();
      expect(requestArg.paid).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    });

    it('should include paid: true in request when checked', () => {
      expenseService.create.mockReturnValue(of(mockExpense));
      component.expenseForm.patchValue({
        description: 'Lunch',
        amount: 20,
        category: 'Meals',
        purchaseDate: new Date(),
        paid: true
      });

      component.onSubmit();

      const requestArg = expenseService.create.mock.calls[0][0];
      expect(requestArg.paid).toBe(true);
    });

    it('should not submit when form is invalid', () => {
      component.expenseForm.reset();
      component.onSubmit();
      expect(expenseService.create).not.toHaveBeenCalled();
    });

    it('should handle create error', () => {
      const error = { error: { message: 'Server error' } };
      expenseService.create.mockReturnValue(throwError(() => error));
      errorService.handleHttpError.mockReturnValue({ message: 'Server error' });
      component.expenseForm.patchValue({
        description: 'Lunch',
        amount: 20,
        category: 'Meals',
        purchaseDate: new Date()
      });

      component.onSubmit();

      expect(errorService.handleHttpError).toHaveBeenCalledWith(error);
      expect(errorService.setError).toHaveBeenCalled();
    });
  });

  describe('edit mode', () => {
    beforeEach(() => {
      expenseService.getById.mockReturnValue(of(mockExpense));
      routeParams$.next({ id: '1' });
    });

    it('should enter edit mode when route has id', () => {
      expect(component.isEditMode).toBe(true);
      expect(component.expenseId).toBe('1');
    });

    it('should populate form without source field', () => {
      expect(component.expenseForm.value.description).toBe('Team lunch');
      expect(component.expenseForm.value.amount).toBe(75.50);
      expect(component.expenseForm.value.category).toBe('Meals');
      expect(component.expenseForm.value.source).toBeUndefined();
      expect(component.expenseForm.value.currency).toBeUndefined();
    });

    it('should populate paid field from expense', () => {
      expect(component.expenseForm.get('paid')!.value).toBe(false);
    });

    it('should populate paid: true when expense is paid', () => {
      expenseService.getById.mockReturnValue(of({ ...mockExpense, paid: true }));
      routeParams$.next({ id: '2' });
      expect(component.expenseForm.get('paid')!.value).toBe(true);
    });

    it('should call update and navigate on valid submit', () => {
      expenseService.update.mockReturnValue(of(mockExpense));

      component.onSubmit();

      expect(expenseService.update).toHaveBeenCalledWith('1', expect.any(Object));
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    });

    it('should include paid in update request', () => {
      expenseService.update.mockReturnValue(of(mockExpense));

      component.onSubmit();

      const requestArg = expenseService.update.mock.calls[0][1];
      expect(requestArg.paid).toBeDefined();
    });
  });

  describe('onCancel', () => {
    it('should navigate to expenses list', () => {
      component.onCancel();
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    });
  });
});
