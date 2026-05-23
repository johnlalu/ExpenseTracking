import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ReportViewComponent } from './report-view.component';
import { ExpenseService } from '../shared/services/expense.service';
import { ErrorService } from '../shared/services/error.service';
import { of, throwError } from 'rxjs';
import { MonthlySummary, Expense } from '../shared/models/expense.model';

describe('ReportViewComponent', () => {
  let component: ReportViewComponent;
  let fixture: ComponentFixture<ReportViewComponent>;
  let expenseService: jasmine.SpyObj<ExpenseService>;
  let errorService: jasmine.SpyObj<ErrorService>;

  const mockSummary: MonthlySummary = {
    month: '2024-05',
    total: 1000,
    totalAmount: 1000,
    count: 10,
    expenseCount: 10,
    categoryBreakdown: { Groceries: 500, Entertainment: 300, Utilities: 200 },
    items: [
      { id: '1', description: 'Grocery shopping', amount: 100, currency: 'USD', category: 'Groceries', purchaseDate: new Date() },
      { id: '2', description: 'Movie', amount: 15, currency: 'USD', category: 'Entertainment', purchaseDate: new Date() }
    ]
  };

  beforeEach(async () => {
    const expenseServiceSpy = jasmine.createSpyObj('ExpenseService', ['getMonthlySummary']);
    const errorServiceSpy = jasmine.createSpyObj('ErrorService', ['handleHttpError', 'setError']);

    await TestBed.configureTestingModule({
      imports: [ReportViewComponent, BrowserAnimationsModule],
      providers: [
        { provide: ExpenseService, useValue: expenseServiceSpy },
        { provide: ErrorService, useValue: errorServiceSpy }
      ]
    }).compileComponents();

    expenseService = TestBed.inject(ExpenseService) as jasmine.SpyObj<ExpenseService>;
    errorService = TestBed.inject(ErrorService) as jasmine.SpyObj<ErrorService>;

    fixture = TestBed.createComponent(ReportViewComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load report on init', () => {
      expenseService.getMonthlySummary.and.returnValue(of(mockSummary));

      component.ngOnInit();

      expect(expenseService.getMonthlySummary).toHaveBeenCalled();
      expect(component.summary).toEqual(mockSummary);
      expect(component.isLoading).toBeFalse();
    });

    it('should handle error when loading report', () => {
      const error = { error: { message: 'Test error' } };
      expenseService.getMonthlySummary.and.returnValue(throwError(() => error));
      errorService.handleHttpError.and.returnValue({ message: 'Test error' } as any);

      component.ngOnInit();

      expect(errorService.handleHttpError).toHaveBeenCalledWith(error);
      expect(errorService.setError).toHaveBeenCalled();
    });
  });

  describe('onMonthChange', () => {
    it('should load report when month changes', () => {
      expenseService.getMonthlySummary.and.returnValue(of(mockSummary));
      const newDate = new Date(2024, 3, 15); // April 2024

      component.onMonthChange(newDate);

      expect(component.selectedMonth).toEqual(newDate);
      expect(expenseService.getMonthlySummary).toHaveBeenCalled();
    });
  });

  describe('buildCategoryBreakdown', () => {
    it('should build category breakdown from expenses', () => {
      const breakdown = component.buildCategoryBreakdown(mockSummary);

      expect(breakdown.length).toBeGreaterThan(0);
      expect(breakdown.some(item => item.name === 'Groceries')).toBeTrue();
    });

    it('should sort categories by amount descending', () => {
      const breakdown = component.buildCategoryBreakdown(mockSummary);

      for (let i = 0; i < breakdown.length - 1; i++) {
        expect(breakdown[i].totalAmount).toBeGreaterThanOrEqual(breakdown[i + 1].totalAmount);
      }
    });

    it('should handle summary with no items', () => {
      const emptySummary: MonthlySummary = { items: [] };
      const breakdown = component.buildCategoryBreakdown(emptySummary);

      expect(breakdown).toEqual([]);
    });
  });

  describe('getAverageAmount', () => {
    it('should calculate average amount correctly', () => {
      component.summary = mockSummary;

      const average = component.getAverageAmount();

      expect(average).toBe(1000 / 10);
    });

    it('should return 0 when summary is null', () => {
      component.summary = null;

      const average = component.getAverageAmount();

      expect(average).toBe(0);
    });

    it('should return 0 when count is 0', () => {
      component.summary = { ...mockSummary, count: 0 };

      const average = component.getAverageAmount();

      expect(average).toBe(0);
    });
  });

  describe('getPercentage', () => {
    it('should calculate percentage correctly', () => {
      component.summary = mockSummary;

      const percentage = component.getPercentage(500);

      expect(percentage).toBe(50); // 500/1000 * 100
    });

    it('should return 0 when summary is null', () => {
      component.summary = null;

      const percentage = component.getPercentage(100);

      expect(percentage).toBe(0);
    });

    it('should return 0 when total amount is 0', () => {
      component.summary = { ...mockSummary, totalAmount: 0 };

      const percentage = component.getPercentage(100);

      expect(percentage).toBe(0);
    });
  });
});
