import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { ExpenseListComponent } from './expense-list.component';
import { ExpenseService } from '../../shared/services/expense.service';
import { ErrorService } from '../../shared/services/error.service';
import { Expense } from '../../shared/models/expense.model';

describe('ExpenseListComponent', () => {
  let component: ExpenseListComponent;
  let fixture: ComponentFixture<ExpenseListComponent>;
  let expenseService: { getByMonth: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let errorService: { handleHttpError: ReturnType<typeof vi.fn>; setError: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const unpaid: Expense = { id: '1', description: 'Lunch', amount: 25, category: 'Meals', purchaseDate: new Date(), paid: false };
  const paid: Expense = { id: '2', description: 'Taxi', amount: 15, category: 'Travel', purchaseDate: new Date(), paid: true };

  beforeEach(async () => {
    expenseService = { getByMonth: vi.fn(), update: vi.fn(), delete: vi.fn() };
    errorService = { handleHttpError: vi.fn(), setError: vi.fn() };
    router = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ExpenseListComponent, BrowserAnimationsModule],
      providers: [
        { provide: ExpenseService, useValue: expenseService },
        { provide: ErrorService, useValue: errorService },
        { provide: Router, useValue: router },
        { provide: MatDialog, useValue: {} }
      ]
    }).compileComponents();
  });

  beforeEach(async () => {
    expenseService.getByMonth.mockReturnValue(of({ items: [{ ...unpaid }, { ...paid }], totalCount: 2, pageSize: 50 }));
    fixture = TestBed.createComponent(ExpenseListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve, 0));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('default state', () => {
    it('should default paidFilter to unpaid', () => {
      expect(component.paidFilter).toBe('unpaid');
    });

    it('should load expenses on init', () => {
      expect(expenseService.getByMonth).toHaveBeenCalled();
      expect(component.expenses.length).toBe(2);
    });
  });

  describe('filteredExpenses', () => {
    it('should return only unpaid when filter is unpaid', () => {
      component.paidFilter = 'unpaid';
      expect(component.filteredExpenses).toEqual([unpaid]);
    });

    it('should return only paid when filter is paid', () => {
      component.paidFilter = 'paid';
      expect(component.filteredExpenses).toEqual([paid]);
    });

    it('should return all when filter is both', () => {
      component.paidFilter = 'both';
      expect(component.filteredExpenses.length).toBe(2);
    });
  });

  describe('allPaid', () => {
    it('should be false when some expenses are unpaid', () => {
      component.paidFilter = 'both';
      expect(component.allPaid).toBe(false);
    });

    it('should be true when all visible expenses are paid', () => {
      component.paidFilter = 'paid';
      expect(component.allPaid).toBe(true);
    });

    it('should be false when expense list is empty', () => {
      component.expenses = [];
      expect(component.allPaid).toBe(false);
    });
  });

  describe('somePaid', () => {
    it('should be true when some but not all are paid', () => {
      component.paidFilter = 'both';
      expect(component.somePaid).toBe(true);
    });

    it('should be false when all visible are paid', () => {
      component.paidFilter = 'paid';
      expect(component.somePaid).toBe(false);
    });

    it('should be false when none are paid', () => {
      component.paidFilter = 'unpaid';
      expect(component.somePaid).toBe(false);
    });
  });

  describe('totalAmount', () => {
    it('should sum only filtered expenses', () => {
      component.paidFilter = 'unpaid';
      expect(component.totalAmount).toBe(25);
    });

    it('should sum all when filter is both', () => {
      component.paidFilter = 'both';
      expect(component.totalAmount).toBe(40);
    });
  });

  describe('togglePaid', () => {
    it('should update expense optimistically', () => {
      expenseService.update.mockReturnValue(of(paid));
      const expense = { ...unpaid };
      component.expenses = [expense];

      component.togglePaid(expense, true);

      expect(expense.paid).toBe(true);
      expect(expenseService.update).toHaveBeenCalledWith('1', expect.objectContaining({ paid: true }));
    });

    it('should revert on API error', () => {
      expenseService.update.mockReturnValue(throwError(() => ({ error: {} })));
      errorService.handleHttpError.mockReturnValue({ message: 'Error' });
      const expense = { ...unpaid };
      component.expenses = [expense];

      component.togglePaid(expense, true);

      expect(expense.paid).toBe(false);
    });
  });

  describe('toggleAll', () => {
    it('should call update for each visible expense', () => {
      expenseService.update.mockReturnValue(of({}));
      component.paidFilter = 'both';

      component.toggleAll(true);

      expect(expenseService.update).toHaveBeenCalledTimes(2);
    });

    it('should only affect filtered expenses', () => {
      expenseService.update.mockReturnValue(of({}));
      component.paidFilter = 'unpaid';

      component.toggleAll(true);

      expect(expenseService.update).toHaveBeenCalledTimes(1);
    });
  });
});
