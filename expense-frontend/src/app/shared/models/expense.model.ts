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
  purchaseDate: Date;
  receiptUrl?: string;
  paid?: boolean;
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
  purchaseDate: Date;
  paid?: boolean;
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
