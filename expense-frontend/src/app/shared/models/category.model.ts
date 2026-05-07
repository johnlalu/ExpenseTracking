/**
 * Category model for expense categorization.
 */
export interface Category {
  id?: string;
  userId?: string;
  name?: string;
  isDefault?: boolean;
  createdAt?: Date;
  isDeleted?: boolean;
}

/**
 * Default categories available to all users.
 */
export const DEFAULT_CATEGORIES = [
  'Travel',
  'Meals',
  'Office Supplies',
  'Technology',
  'Accommodation',
  'Transportation',
  'Other'
];
