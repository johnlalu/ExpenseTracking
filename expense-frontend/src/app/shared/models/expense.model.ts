/**
 * Expense model representing transaction data.
 */
export interface Expense {
  id?: string;
  userId?: string;
  description?: string;
  amount: number;
  currency: string;
  category?: string;
  source?: string;
  purchaseDate: Date;
  receiptUrl?: string;
  createdAt?: Date;
  updatedAt?: Date;
}

/**
 * Request body for creating an expense.
 */
export interface CreateExpenseRequest {
  description?: string;
  amount: number;
  currency: string;
  category?: string;
  source?: string;
  purchaseDate: Date;
}

/**
 * Response containing paginated expenses.
 */
export interface ExpenseListResponse {
  items: Expense[];
  totalCount: number;
  pageSize: number;
}

/**
 * Category breakdown for reports.
 */
export interface CategoryBreakdown {
  name: string;
  totalAmount: number;
}
