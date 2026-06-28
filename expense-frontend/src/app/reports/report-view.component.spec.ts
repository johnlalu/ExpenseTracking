import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ReportViewComponent } from './report-view.component';
import { ExpenseService } from '../shared/services/expense.service';
import { ErrorService } from '../shared/services/error.service';
import { of, throwError } from 'rxjs';
import { Expense, ExpenseListResponse } from '../shared/models/expense.model';

describe('ReportViewComponent', () => {
  let component: ReportViewComponent;
  let fixture: ComponentFixture<ReportViewComponent>;
  let expenseService: { getByMonth: ReturnType<typeof vi.fn> };
  let errorService: { handleHttpError: ReturnType<typeof vi.fn>; setError: ReturnType<typeof vi.fn> };

  const mockExpenses: Expense[] = [
    { id: '1', description: 'Grocery shopping', amount: 400, currency: 'USD', category: 'Groceries', purchaseDate: new Date() },
    { id: '2', description: 'Movie', amount: 15, currency: 'USD', category: 'Entertainment', purchaseDate: new Date() },
    { id: '3', description: 'More groceries', amount: 100, currency: 'USD', category: 'Groceries', purchaseDate: new Date() }
  ];

  const mockResponse: ExpenseListResponse = { items: mockExpenses, totalCount: 3, pageSize: 50 };

  beforeEach(async () => {
    expenseService = { getByMonth: vi.fn() };
    errorService = { handleHttpError: vi.fn(), setError: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ReportViewComponent, BrowserAnimationsModule],
      providers: [
        { provide: ExpenseService, useValue: expenseService },
        { provide: ErrorService, useValue: errorService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ReportViewComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expenseService.getByMonth.mockReturnValue(of(mockResponse));
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  describe('loadReport', () => {
    it('should load report on init', () => {
      expenseService.getByMonth.mockReturnValue(of(mockResponse));

      component.ngOnInit();

      expect(expenseService.getByMonth).toHaveBeenCalled();
      expect(component.summary).toEqual({ totalAmount: 515, count: 3 });
      expect(component.isLoading).toBe(false);
    });

    it('should set summary to null when no items returned', () => {
      expenseService.getByMonth.mockReturnValue(of({ items: [], totalCount: 0, pageSize: 50 }));

      component.ngOnInit();

      expect(component.summary).toBeNull();
      expect(component.categoryBreakdown).toEqual([]);
    });

    it('should handle error when loading report', () => {
      const error = { error: { message: 'Test error' } };
      expenseService.getByMonth.mockReturnValue(throwError(() => error));
      errorService.handleHttpError.mockReturnValue({ message: 'Test error' });

      component.ngOnInit();

      expect(errorService.handleHttpError).toHaveBeenCalledWith(error);
      expect(errorService.setError).toHaveBeenCalled();
    });
  });

  describe('onMonthChange', () => {
    it('should reload report when month changes', () => {
      expenseService.getByMonth.mockReturnValue(of(mockResponse));
      const newDate = new Date(2024, 3, 15);

      component.onMonthChange(newDate);

      expect(component.selectedMonth).toEqual(newDate);
      expect(expenseService.getByMonth).toHaveBeenCalledWith(4, 2024);
    });
  });

  describe('buildCategoryBreakdown', () => {
    it('should group expenses by category', () => {
      const breakdown = component.buildCategoryBreakdown(mockExpenses);
      expect(breakdown.length).toBe(2);
      expect(breakdown.some(item => item.name === 'Groceries')).toBe(true);
    });

    it('should sort categories by amount descending', () => {
      const breakdown = component.buildCategoryBreakdown(mockExpenses);
      for (let i = 0; i < breakdown.length - 1; i++) {
        expect(breakdown[i].totalAmount).toBeGreaterThanOrEqual(breakdown[i + 1].totalAmount);
      }
    });

    it('should return empty array for no expenses', () => {
      expect(component.buildCategoryBreakdown([])).toEqual([]);
    });
  });

  describe('getAverageAmount', () => {
    it('should calculate average amount correctly', () => {
      component.summary = { totalAmount: 1000, count: 10 };
      expect(component.getAverageAmount()).toBe(100);
    });

    it('should return 0 when summary is null', () => {
      component.summary = null;
      expect(component.getAverageAmount()).toBe(0);
    });

    it('should return 0 when count is 0', () => {
      component.summary = { totalAmount: 1000, count: 0 };
      expect(component.getAverageAmount()).toBe(0);
    });
  });

  describe('getPercentage', () => {
    it('should calculate percentage correctly', () => {
      component.summary = { totalAmount: 1000, count: 5 };
      expect(component.getPercentage(500)).toBe(50);
    });

    it('should return 0 when summary is null', () => {
      component.summary = null;
      expect(component.getPercentage(100)).toBe(0);
    });

    it('should return 0 when total amount is 0', () => {
      component.summary = { totalAmount: 0, count: 5 };
      expect(component.getPercentage(100)).toBe(0);
    });
  });
});
