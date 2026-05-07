Perfect! I'll provide the complete plan as a markdown block that you can copy into your `ExpensesProjectSpecs.md` file:

```markdown
# Expense Reimbursement Tracking Application - Implementation Plan

**Last Updated:** April 18, 2026

## Executive Summary

Build a single-user expense tracking app with **Angular 20** frontend, **.NET 10** backend, and Azure Cosmos DB. Use **App Service for REST API** (zero cold starts) + **Azure Functions** for async background jobs (receipt processing, reporting). Deploy to Azure with hybrid serverless/traditional approach. JWT authentication, structured logging with Application Insights from day one.

---

## Requirements

- **Scope**: Single-user personal expense tracking
- **Tech Stack**: 
  - Frontend: Angular 20
  - Backend: .NET 10 Core
  - Database: Azure Cosmos DB (SQL API)
  - Hosting: Azure App Service + Azure Functions (Flex Consumption)
  - Storage: Azure Blob Storage (receipt images)
  - Logging: Serilog + Application Insights
- **Authentication**: JWT with email/password
- **Core Features**: Add/edit/view expenses, grid by month (default current month)
- **Extended Features**: CSV/Excel export, receipt image upload, categories/tags, reporting/analytics
- **Quality Standards**: Best practices for error handling, logging, testing (80%+ code coverage), linting enforcement

---

## Architecture Overview

```
┌─────────────────────────────────┐
│   Angular 20 Frontend (SPA)      │
├─────────────────────────────────┤
│ • Material Design UI             │
│ • Reactive Forms (Signal-based)  │
│ • Error handling + logging       │
│ • JWT authentication             │
└──────────────┬────────────────────┘
               │
        ┌──────┴──────┐
        ▼             ▼
┌──────────────┐  ┌────────────────────────┐
│  App Service │  │ Azure Functions (Flex) │
│ (.NET 10)    │  │                        │
├──────────────┤  ├────────────────────────┤
│ REST API     │  │ Receipt Processing     │
│ Controllers  │  │ Monthly Aggregation    │
│ Services     │  │ Scheduled Jobs         │
│ Repositories │  │ Cosmos DB bindings     │
└───────┬──────┘  └────────────────────────┘
        │
        └─────────────┬─────────────────────┐
                      ▼                     ▼
            ┌──────────────────┐   ┌────────────────┐
            │ Azure Cosmos DB  │   │ Blob Storage   │
            │                  │   │                │
            │ SQL API          │   │ Receipt Images │
            │ userId partition │   │ RA-GRS repl.   │
            └──────────────────┘   └────────────────┘
                      │
                      ▼
            ┌──────────────────┐
            │ Application      │
            │ Insights         │
            │                  │
            │ Centralized      │
            │ Logging/Monitor  │
            └──────────────────┘
```

---

## Implementation Phases

### Phase 1: Foundation & Authentication (Parallel work)

**Objective**: Project structure, authentication, and base infrastructure

**Tasks**:

1. **Initialize Angular 20 Project**
   - Create new project: `ng new expense-reimbursement --strict`
   - Configure TypeScript strict mode
   - Setup ESLint + Prettier for code quality
   - Install Material Design: `ng add @angular/material`
   - Create file structure:
     - `src/app/auth/` — Login, Register components
     - `src/app/expenses/` — CRUD components
     - `src/app/categories/` — Category management
     - `src/app/reports/` — Analytics/charts
     - `src/app/shared/` — Interceptors, services, models, components
   - Install dependencies: `@angular/common/http`, `Chart.js`, `ExcelJS`

2. **Initialize .NET 10 Web API**
   - Create new project: `dotnet new webapi -n ExpenseApi`
   - Target .NET 10
   - Project structure:
     - `Controllers/` — AuthController, ExpensesController
     - `Services/` — AuthService, ExpenseService, CategoryService
     - `Data/` — Repository pattern, ExpenseRepository
     - `Models/` — Expense, Category domain models
     - `Middleware/` — Exception handling, JWT validation
     - `Validation/` — FluentValidation rules
     - `Logging/` — Serilog extensions
     - `Tests/Unit/` — xUnit test files
     - `Tests/Integration/` — Cosmos DB integration tests
   - NuGet packages:
     - `Microsoft.Azure.Cosmos` (3.58.0+)
     - `Serilog` + `Serilog.Sinks.ApplicationInsights`
     - `FluentValidation`
     - `xUnit`, `Moq`
     - `AutoMapper` (optional)

3. **Create Azure Resources**
   - Cosmos DB SQL API account
     - Database: `ExpenseDB`
     - Container: `Expenses` (partition key: `/userId`, RU: auto-scale 400-4000)
     - Enable continuous backup (30 days)
   - Blob Storage account
     - Container: `receipts` (private, RA-GRS replication)
   - Application Insights instance (linked to App Service)
   - App Service Plan (B1 tier, ~$50-80/month)
   - Azure Functions storage account (Flex Consumption)

4. **.NET 10 JWT Authentication Implementation**
   - **AuthController**:
     - `POST /api/auth/register` — Hash password (bcrypt), store user in database or managed identity
     - `POST /api/auth/login` — Validate credentials, return JWT + refresh token
     - `POST /api/auth/refresh` — Issue new token with refresh token rotation
   - **JWT generation**:
     - Claims: `userId` (email), `sub`, `exp`, `iat`
     - Token expiration: 15 minutes (access), 7 days (refresh)
     - Store refresh tokens in Cosmos DB with expiration
   - **Middleware**:
     - Create `JwtValidationMiddleware` to validate token on protected routes
     - Return 401 for invalid/expired tokens
   - **Secret management**: Use Azure Key Vault for JWT signing key

5. **Angular 20 Authentication**
   - **AuthService**:
     - Methods: `register()`, `login()`, `logout()`, `refreshToken()`
     - Store JWT in memory (not localStorage), refresh token in httpOnly cookie
     - BehaviorSubject for auth state
   - **AuthGuard**: Protect routes, redirect to login if not authenticated
   - **Login Component**: Form with email/password, validation, error display
   - **Register Component**: Similar form with password confirmation
   - **HTTP Interceptor (auth.interceptor.ts)**:
     - Attach JWT to all requests: `Authorization: Bearer {token}`
     - Handle 401 responses by attempting silent refresh
     - If refresh fails, redirect to login

6. **Error Handling & Logging Bootstrap**
   - **.NET Global Exception Handling**:
     - `ExceptionHandlingMiddleware`: Catches all unhandled exceptions
     - Generates unique `correlationId` for each request
     - Logs to Serilog with full context (UserId, RequestId, endpoint, exception details)
     - Returns standardized error response: `{ message: string, logId: string, statusCode: int }`
     - Never expose stack traces to client (log internally only)
   - **Serilog Configuration**:
     ```csharp
     Log.Logger = new LoggerConfiguration()
         .MinimumLevel.Information()
         .Enrich.WithProperty("Application", "ExpenseReimbursement")
         .Enrich.WithMachineName()
         .Enrich.WithThreadId()
         .Enrich.FromLogContext()
         .WriteTo.ApplicationInsights(
             new TelemetryClient(),
             TelemetryConverter.Events)
         .CreateLogger();
     ```
   - **Angular Error Handling**:
     - `ErrorService`: Centralized error state management
     - `error.interceptor.ts`: Catches HTTP errors, transforms to user-friendly messages
     - `ErrorModalComponent`: Displays error with Log ID, "Copy to Clipboard" button for error + Log ID
     - Toast/modal notifications for different error types

7. **Application Insights Integration**
   - Configure App Insights connection string in App Service settings
   - Configure App Insights SDK in Angular (ApplicationInsightsModule)
   - Both frontend and backend log to same Application Insights instance
   - Logs include: Request ID, User ID, Operation name, Duration, Success/Failure
   - Set up basic dashboard: Request rate, exception rate, response time

**Verification**:
- Angular 20 development server loads without errors
- Navigate to login page
- Create new user account via register endpoint
- Login returns JWT token (verify in browser DevTools)
- Protected endpoint (e.g., GET /api/expenses) returns 401 without token, 200 with valid token
- Login with valid credentials stores token and redirects to dashboard
- Manual logout clears token and redirects to login
- Exceptions logged to Application Insights with correlationId visible in portal

---

### Phase 2: Core Data Model & REST API (Depends on Phase 1)

**Objective**: Implement CRUD operations for expenses with validation and error handling

**Tasks**:

1. **Cosmos DB Schema Design**
   - **Expense Document**:
     ```json
     {
       "id": "550e8400-e29b-41d4-a716-446655440000",
       "userId": "user@example.com",
       "description": "Team lunch",
       "amount": 45.50,
       "currency": "USD",
       "category": "Meals",
       "purchaseDate": "2026-04-15T00:00:00Z",
       "source": "Restaurant ABC",
       "receiptUrl": "https://...blob.core.windows.net/receipts/...",
       "createdAt": "2026-04-18T10:30:00Z",
       "updatedAt": "2026-04-18T10:30:00Z"
     }
     ```
   - **Partition Key**: `/userId` (enables user isolation, scales well)
   - **Indexes**: Create composite index on `{category, purchaseDate}` for filtering
   - **TTL**: Optional 90-day expiration for archived expenses

2. **ExpenseRepository (Cosmos DB Implementation)**
   - Implement `IExpenseRepository` interface with methods:
     - `CreateAsync(Expense)` → returns created item with generated id
     - `GetByIdAsync(id, userId)` → fetch single expense (include userId in query for security)
     - `GetByMonthYearAsync(userId, month, year)` → return all expenses for month
     - `GetByDateRangeAsync(userId, startDate, endDate)` → for filtering
     - `GetByCategoryAsync(userId, category)` → for category filtering
     - `UpdateAsync(id, userId, updatedExpense)` → transactional update
     - `DeleteAsync(id, userId)` → soft delete (mark isDeleted flag)
   - Use `.NET 10 Cosmos SDK 3.58.0+` with typed queries
   - Implement proper error handling for CosmosException (409 conflicts, 429 rate limits)
   - Log RU consumption and operation time for monitoring
   - All queries include `WHERE userId = @userId` for data isolation

3. **ExpenseService (Business Logic)**
   - Dependency inject ExpenseRepository
   - Methods mirror repository but add business logic:
     - `CreateExpenseAsync()` — validate input, log creation, return DTO
     - `GetExpensesForMonthAsync()` — query repository, map to DTOs
     - `UpdateExpenseAsync()` — validate updates, check for conflicts, log change
     - `DeleteExpenseAsync()` — validate ownership, soft delete, log deletion
     - `GetMonthlySummaryAsync()` — aggregate expenses by month for reporting
   - Use Serilog for all operations: `_logger.LogInformation("Expense {id} created by {userId}", ...)`
   - Return domain exceptions on business rule violations

4. **Input Validation (FluentValidation)**
   - Create `ExpenseValidator`:
     ```csharp
     public class ExpenseValidator : AbstractValidator<CreateExpenseRequest>
     {
         public ExpenseValidator()
         {
             RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
             RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(10000);
             RuleFor(x => x.PurchaseDate).LessThanOrEqualTo(DateTime.UtcNow);
             RuleFor(x => x.Category).NotEmpty();
         }
     }
     ```
   - Inject validator in service, run before creating/updating
   - Return 400 Bad Request with field-level error messages
   - Include validation error response DTO: `{ field: string, message: string }[]`

5. **REST API Endpoints**
   - **ExpensesController** in App Service:
     - `POST /api/expenses` — Create (request body: CreateExpenseRequest)
       - Validate input, call service, return 201 Created with location header
     - `GET /api/expenses?month=04&year=2026` — List by month (query params)
       - Default to current month if not provided
       - Return paged results: `{ items: Expense[], totalCount: int, pageSize: int }`
     - `GET /api/expenses/{id}` — Get single expense
       - Validate userId ownership before returning
       - Return 404 if not found or unauthorized
     - `PUT /api/expenses/{id}` — Update expense (request body: UpdateExpenseRequest)
       - Validate ownership, validate input, update, return 200 OK
     - `DELETE /api/expenses/{id}` — Delete expense
       - Soft delete, log deletion, return 204 No Content
     - `GET /api/expenses/report/monthly` — Monthly summary for charts
       - Aggregate by month, return: `{ month: string, total: decimal, categoryBreakdown: {...} }[]`
   - **CategoryController**:
     - `GET /api/categories` — List default + user categories
     - `POST /api/categories` — Create custom category
     - `DELETE /api/categories/{id}` — Delete category (soft delete)
   - **AuthController**:
     - `POST /api/auth/register`
     - `POST /api/auth/login`
     - `POST /api/auth/refresh`

6. **Error Responses**
   - Standardized format:
     ```json
     {
       "message": "User-friendly error message",
       "logId": "correlationId",
       "statusCode": 400,
       "details": [
         { "field": "amount", "message": "Amount must be greater than 0" }
       ]
     }
     ```
   - Example error responses:
     - 400 Bad Request — validation failed
     - 401 Unauthorized — missing/invalid JWT
     - 403 Forbidden — user trying to access another user's expense
     - 404 Not Found — expense doesn't exist
     - 500 Internal Server Error — log full exception, return generic message with logId

**Verification**:
- All CRUD endpoints respond with correct HTTP status codes
- Create expense returns 201 with location header
- List endpoint filters by month correctly (current month by default)
- Update endpoint persists changes
- Delete endpoint soft-deletes
- Validation errors return 400 with field-level messages
- All exceptions logged to Application Insights with correlationId
- Only authenticated users can access endpoints (401 without token)
- Users can only access their own expenses (403 if trying to access another user's expense)
- Monthly summary aggregation works correctly

---

### Phase 3: Angular UI & Client Logic (Depends on Phase 2)

**Objective**: Build user interface and integrate with API

**Tasks**:

1. **Core Components**
   - **DashboardComponent** (main layout):
     - Sidenav navigation, header with logout button
     - Main content area (router outlet)
     - Routes: expenses, categories, reports
   - **ExpenseListComponent** (grid view):
     - Table with columns: Description, Amount, Category, Date, Actions (Edit/Delete)
     - Month/year selector above grid (default current month)
     - Load expenses on month change
     - Loading spinner while fetching
     - Edit button opens ExpenseFormComponent in modal
     - Delete button shows confirmation dialog
     - Paginated results (10-20 items per page)
   - **ExpenseFormComponent** (modal/page):
     - Reactive form with fields: Description, Amount, Currency, Category, PurchaseDate, Source, Receipt
     - Use Angular 20's Signal-based reactive forms: `form = toSignal(this.fb.group(...))`
     - Validation messages appear inline
     - Submit button disabled until form valid
     - Cancel button closes modal
     - For edit: pre-populate form fields
   - **CategoryManagementComponent**:
     - List default categories (read-only)
     - List user categories with delete buttons
     - Form to add new category
   - **ReportingComponent**:
     - Charts using Chart.js
     - Date range picker
     - Display: Total by month (line chart), by category (pie chart)
   - **LoginComponent** & **RegisterComponent**:
     - Email/password forms with validation
     - Submit to auth endpoints
     - Error messages from backend
     - Redirect to dashboard on success
   - **ErrorModalComponent** (shared):
     - Display error message, Log ID, copy button
     - Triggered by ErrorService when API errors occur
     - Copy copies: `Error: {message} | Log ID: {logId}`

2. **Services**
   - **ExpenseService**:
     - Inject HttpClient
     - Methods: `create()`, `getByMonth()`, `update()`, `delete()`, `getMonthlySummary()`
     - Observable-based (RxJS), use `shareReplay()` for caching
   - **CategoryService**:
     - Methods: `getAll()`, `create()`, `delete()`
   - **AuthService** (from Phase 1, enhanced):
     - Add `getCurrentUser()` method
     - Manage token refresh
   - **ErrorService**:
     - Observable stream for error events
     - Components subscribe to show error modal
     - Called by error.interceptor

3. **Interceptors**
   - **auth.interceptor.ts**:
     - Add `Authorization: Bearer {token}` header to all requests
     - Handle 401 by calling refresh endpoint
     - If refresh fails, clear token and redirect to login
   - **error.interceptor.ts**:
     - Catch HttpErrorResponse
     - Transform to user-friendly message
     - Emit error via ErrorService
     - Return throwError so component can handle optionally
   - **logging.interceptor.ts**:
     - Log request/response for debugging
     - Include correlation ID in requests (from backend response)

4. **State Management (RxJS)**
   - **ExpenseStore** (simple service-based):
     - `expensesSubject = new BehaviorSubject<Expense[]>([])`
     - `selectedMonthSubject = new BehaviorSubject<{month, year}>(current)`
     - Methods: `setSelectedMonth()`, `refreshExpenses()`
     - `expensesList$ = expensesSubject.asObservable()`
     - Components subscribe and use async pipe in templates
     - Cache monthly data to avoid redundant API calls

5. **Forms & Validation**
   - Use Reactive Forms (FormBuilder)
   - Signal-based approach in Angular 20:
     ```typescript
     form = toSignal(this.fb.group({
       description: ['', Validators.required],
       amount: [0, [Validators.required, Validators.min(0.01)]],
       category: ['', Validators.required],
       purchaseDate: [new Date(), Validators.required]
     }));
     ```
   - Custom validators: `uniqueExpensePerDay()`, `validDate()`
   - Display validation errors inline (error messages appear when field touched)
   - Async validators: Check duplicate expense descriptions on same day (optional)

6. **Responsive Design**
   - Mobile-first approach
   - Material breakpoints: xs (<600px), sm (600-960px), md (960-1200px), lg (>1200px)
   - Grid responsive: Single column on mobile, multi-column on desktop
   - Sidenav collapses to hamburger menu on mobile
   - Forms full-width on all sizes

7. **Features**
   - Month/year selector with dropdown or stepper
   - Default to current month on load
   - In-grid editing via modal (click row or edit button)
   - Inline category selector (dropdown in form)
   - Amount formatting (currency symbol, 2 decimals)
   - Date formatting (relative dates or locale format)

**Verification**:
- Login page loads, can create account and login
- Dashboard displays after successful login
- Expense grid loads and shows current month
- Month/year selector changes data displayed
- Add button opens form, submission creates expense and refreshes grid
- Edit button opens form with pre-populated data, update persists
- Delete button shows confirmation, soft delete removes from grid
- Form validation prevents invalid submission
- Error messages display for field validation
- All API errors show friendly message + copy-to-clipboard Log ID
- Responsive layout on mobile and desktop
- Logout clears token and redirects to login

---

### Phase 4: Advanced Features (Depends on Phase 3)

**Objective**: Add receipt uploads, categories, exports, reporting

**Tasks**:

1. **Receipt Image Upload (Blob Storage)**
   - **.NET Implementation**:
     - `BlobStorageService`: Methods for `UploadAsync()`, `DeleteAsync()`
     - Use `Azure.Storage.Blobs` NuGet package
     - Generate unique blob name: `{userId}/{expenseId}/{filename}`
     - Validate file: 5MB max, formats (JPG, PNG, PDF)
     - Return full blob URI to client
     - Store URI in Expense document
     - Use Managed Identity for authentication (no connection strings)
   - **Angular Implementation**:
     - Add file input to expense form
     - Show file preview (image for JPG/PNG, placeholder for PDF)
     - Upload on form submit (call backend endpoint)
     - Show progress bar during upload
     - Display thumbnail in expense grid
     - Allow delete receipt (removes blob)
   - **Endpoint**:
     - `POST /api/expenses/{id}/receipt` — upload file
     - `DELETE /api/expenses/{id}/receipt` — remove receipt

2. **Categories Management**
   - **Database Schema**:
     - Category document: `{ id, userId, name, createdAt }`
     - Default categories: Travel, Meals, Office Supplies, Other (application-hardcoded)
   - **.NET**:
     - `CategoryRepository` with CRUD methods
     - `CategoryService` with business logic
     - `CategoryValidator` to ensure unique names per user
   - **Angular**:
     - CategoryManagementComponent with form to add new category
     - List default categories (read-only)
     - List custom categories with delete buttons
     - Expense form has category dropdown (populated from service)
     - Filter expenses by category

3. **CSV/Excel Export**
   - **.NET Implementation** (optional, can be client-side):
     - `POST /api/expenses/export` — Returns CSV or Excel file
     - Query parameters: `?startDate=&endDate=&category=`
     - Generate XLSX using EPPlus or ClosedXml
     - Columns: Description, Amount, Currency, Date, Category, Source, ReceiptUrl
   - **Angular Implementation**:
     - "Export" button in expense grid
     - Calls backend export endpoint with current filters
     - Browser downloads file: `ExpenseReport_YYYY-MM.xlsx`
     - Alternative: Use ExcelJS library to generate on client-side
   - File format: Excel (.xlsx) with formatting (headers, number formatting, autofit columns)

4. **Reporting & Analytics**
   - **Charts** (Chart.js):
     - **Monthly Total Chart** (line chart): Sum by month over past 12 months
       - X-axis: Month/Year
       - Y-axis: Total amount
     - **Category Breakdown** (pie/donut chart): Sum by category for selected month
       - Display category name and amount
     - **Top Categories** (bar chart): Total by category for selected month
   - **Filters**:
     - Date range picker (start/end date)
     - Category filter (dropdown or multi-select)
     - Apply filters button to refresh charts
   - **Data**:
     - Call `GET /api/expenses/report/monthly` endpoint
     - Backend aggregates and returns summary data
     - Frontend renders charts
   - **ReportingComponent**:
     - Date range inputs
     - Chart components (one for each chart type)
     - Summary cards: Total spent, average expense, expense count

5. **Search & Advanced Filtering**
   - **Angular Component**:
     - Search box: full-text search on description
     - Filters: date range, category, amount range
     - Clear all filters button
   - **.NET Implementation** (optional):
     - `GET /api/expenses/search?query=lunch` — full-text search
     - Implement Cosmos DB LIKE operator for description matching
   - **UI Integration**:
     - Filter panel above expense grid
     - Grid updates on filter change
     - Display active filter badges (e.g., "Category: Meals")

**Verification**:
- Upload receipt image, see thumbnail in grid
- Delete receipt removes from blob storage and Expense document
- Create custom category in category management
- Filter expenses by custom category
- Export to Excel downloads file with all expenses
- Open exported Excel file, verify formatting and data
- Charts load with correct data
- Date range picker filters chart data
- Search box finds expenses by description
- Filter badges display active filters
- Clear filters resets to default view

---

### Phase 5: Background Jobs & Async Processing (Parallel with Phase 4)

**Objective**: Implement Azure Functions for async receipt processing and scheduled tasks

**Tasks**:

1. **Receipt Processing Function**
   - **Create Azure Function** (Flex Consumption Plan):
     - Function name: `ReceiptProcessorFunction`
     - Trigger: HTTP trigger (called by Angular on receipt upload)
     - Input: Upload receipt image to Blob Storage, get blobUri
     - Processing:
       - Validate image format and size (duplicate validation)
       - Store image in Blob Storage
       - Optional: Call Azure Computer Vision API for OCR (extract amount/date)
       - Update Expense document in Cosmos DB with receiptUrl
     - Output: Return success/failure to Angular
     - Error handling: Log to Application Insights, return friendly error
   - **Cosmos DB Binding**:
     - Use Azure Functions Cosmos DB binding for easy document updates
     - Binding input: Fetch Expense document by ID
     - Binding output: Update Expense with receiptUrl
   - **Code**:
     ```csharp
     [FunctionName("ReceiptProcessor")]
     public async Task<IActionResult> Run(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = "expenses/{id}/receipt")] 
         HttpRequest req,
         [CosmosDB("ExpenseDB", "Expenses", ConnectionStringSetting = "CosmosDbConnection")]
         IAsyncCollector<Expense> output,
         string id,
         ILogger log)
     {
         // Validate and process receipt
         // Update Expense document
         // Return 200 OK
     }
     ```

2. **Monthly Summary Function** (Scheduled)
   - **Function name**: `MonthlySummaryFunction`
   - **Trigger**: Timer trigger (runs daily at 00:00 UTC)
   - **Processing**:
     - Query all expenses for previous month
     - Aggregate: total, by category, count
     - Store summary in Cosmos DB or export to Blob Storage
     - Log completion to Application Insights
   - **Code**:
     ```csharp
     [FunctionName("MonthlySummary")]
     public async Task Run(
         [TimerTrigger("0 0 0 * * *")] TimerInfo myTimer,
         [CosmosDB(ConnectionStringSetting = "CosmosDbConnection")]
         CosmosClient cosmosClient,
         ILogger log)
     {
         // Query expenses from previous month
         // Aggregate and store results
         // Log to Application Insights
     }
     ```

3. **Cosmos DB Change Feed Processor** (Optional Audit Trail)
   - Implement change feed processor to track all mutations
   - Store audit entries: { documentId, userId, action, timestamp, newValue, oldValue }
   - Useful for compliance and debugging
   - Can run as Function or background service in App Service

4. **Function App Configuration**
   - `local.settings.json`:
     ```json
     {
       "IsEncrypted": false,
       "Values": {
         "AzureWebJobsStorage": "UseDevelopmentStorage=true",
         "FUNCTIONS_WORKER_RUNTIME": "dotnet",
         "CosmosDbConnection": "AccountEndpoint=..."
       }
     }
     ```
   - Deploy to Flex Consumption Plan
   - Use Managed Identity for Cosmos DB and Blob Storage access
   - Configure Application Insights connection string

5. **Error Handling in Functions**
   - Catch exceptions, log to Application Insights
   - Return appropriate HTTP status codes (400, 500)
   - Implement retry logic for transient failures
   - Use Polly library for resilience patterns

**Verification**:
- Upload receipt, Function processes asynchronously
- Expense document updated with receiptUrl
- Function execution logged in Application Insights
- Monthly summary generated on schedule
- Summary data accessible in Cosmos DB
- Function errors logged with context
- Audit trail entries created for document mutations

---

### Phase 6: Testing & Code Quality (Throughout, finalized after Phase 4)

**Objective**: Achieve 80%+ code coverage, enforce linting, ensure production quality

**Tasks**:

1. **Angular 20 Unit Tests (Jasmine/Karma)**
   - **AuthService Tests**:
     - `should register user and return token`
     - `should login with valid credentials`
     - `should refresh token on 401 response`
     - `should clear token on logout`
     - `should emit auth state changes`
   - **ExpenseService Tests**:
     - `should create expense and return created item`
     - `should fetch expenses for month`
     - `should update expense`
     - `should delete expense`
     - `should handle API errors and emit to ErrorService`
   - **Component Tests**:
     - **LoginComponent**: Form submission, validation, error display
     - **ExpenseListComponent**: Load expenses, pagination, month change, edit/delete actions
     - **ExpenseFormComponent**: Form validation, Signal-based reactive forms, submit
     - **ReportingComponent**: Chart rendering, date range filtering
   - **Interceptor Tests**:
     - **auth.interceptor**: Adds JWT header, handles 401 refresh
     - **error.interceptor**: Catches errors, emits to ErrorService
   - **Test Patterns** (AAA - Arrange, Act, Assert):
     ```typescript
     describe('ExpenseService', () => {
       let service: ExpenseService;
       let httpMock: HttpTestingController;

       beforeEach(() => {
         TestBed.configureTestingModule({
           imports: [HttpClientTestingModule],
           providers: [ExpenseService]
         });
         service = TestBed.inject(ExpenseService);
         httpMock = TestBed.inject(HttpTestingController);
       });

       afterEach(() => httpMock.verify());

       it('should create expense', () => {
         const mockExpense = { id: '1', description: 'Lunch', amount: 50 };
         service.create(mockExpense).subscribe(result => {
           expect(result).toEqual(mockExpense);
         });
         const req = httpMock.expectOne('/api/expenses');
         req.flush(mockExpense);
       });
     });
     ```
   - **Coverage Target**: 80% (statements, branches, functions, lines)
   - **Run Tests**: `ng test --code-coverage`

2. **.NET 10 Unit Tests (xUnit + Moq)**
   - **Repository Tests** (with mocked Cosmos DB):
     - `CreateAsync_WithValidExpense_ReturnsCreatedItem`
     - `GetByIdAsync_WithInvalidId_ThrowsNotFoundException`
     - `GetByMonthYearAsync_ReturnsExpensesForMonth`
     - `UpdateAsync_WithConflict_ThrowsApplicationException`
     - Test error scenarios: 409 Conflict, 429 Rate Limit
   - **Service Tests** (with mocked Repository):
     - `CreateExpenseAsync_WithValidInput_CreatesAndLogs`
     - `UpdateExpenseAsync_WithInvalidOwner_Returns403`
     - `DeleteExpenseAsync_WithInvalidId_Returns404`
     - Verify Serilog.Log was called
   - **Controller Tests** (with mocked Services):
     - `CreateExpense_WithValidRequest_Returns201`
     - `GetExpenses_WithoutToken_Returns401`
     - `GetExpenses_WithOtherUserId_Returns403`
     - Verify service methods called with correct parameters
   - **Validation Tests**:
     - `ExpenseValidator_WithInvalidAmount_FailsValidation`
     - `ExpenseValidator_WithEmptyDescription_FailsValidation`
   - **Test Pattern**:
     ```csharp
     public class ExpenseRepositoryTests
     {
       private readonly Mock<CosmosClient> _mockCosmosClient;
       private readonly ExpenseRepository _repository;

       public ExpenseRepositoryTests()
       {
         _mockCosmosClient = new Mock<CosmosClient>();
         // Setup mocks
         _repository = new ExpenseRepository(_mockCosmosClient.Object);
       }

       [Fact]
       public async Task CreateAsync_WithValidExpense_ReturnsCreatedItem()
       {
         // Arrange
         var expense = new Expense { Id = "1", Amount = 100 };
         var mockResponse = new Mock<ItemResponse<Expense>>();
         mockResponse.Setup(r => r.Resource).Returns(expense);
         
         _mockCosmosClient
           .Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
           .Returns(_mockContainer.Object);

         // Act
         var result = await _repository.CreateAsync(expense);

         // Assert
         Assert.Equal("1", result.Id);
         _mockContainer.Verify(c => c.CreateItemAsync(...), Times.Once);
       }
     }
     ```
   - **Coverage Target**: 80%
   - **Run Tests**: `dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura`

3. **Integration Tests**
   - **Angular E2E** (Cypress or Playwright):
     - `Register → Login → Create Expense → Verify in Grid → Export`
     - `Edit Expense → Verify Update`
     - `Delete Expense → Verify Removal`
     - `Filter by Month → Verify Results`
     - `Upload Receipt → Verify Thumbnail`
   - **.NET Integration** (with Cosmos DB Emulator):
     - Full CRUD workflow with real Cosmos DB connection
     - Transaction tests: Create multiple expenses, verify all-or-nothing
     - Rate limit handling
     - Start Cosmos DB Emulator for local testing
   - **Setup**:
     - Use `TestContainers` library for CI/CD pipeline Cosmos DB
     - Create `appsettings.test.json` with emulator connection string
     - Seed test data in `[ClassInitialize]` / `[OneTimeSetUp]`

4. **Code Quality & Linting**
   - **Angular**:
     - `.eslintrc.json`: Enable strict rules
       - No any types
       - No console logs (except service)
       - Enforce naming conventions
     - Run: `ng lint --fix` to auto-fix
     - Pre-commit hook: Block commit if linting fails
   - **.NET**:
     - `.editorconfig`: Define code style
     - StyleCop rules: PascalCase for public members, documented public APIs
     - Run: `dotnet format --verify-no-changes` in CI
     - Code analysis: Enable FxCop / Roslyn analyzers
   - **Configuration**:
     - Prettier for TypeScript/Angular formatting
     - Husky + lint-staged for pre-commit hooks
     - CI pipeline enforces linting (fail build if linting errors)

5. **Coverage Gates & CI Integration**
   - Set minimum coverage threshold: 80%
   - Fail build if coverage drops below threshold
   - Track coverage trends in CI dashboard
   - Generate coverage reports (HTML) after tests
   - Post coverage to PR comments (GitHub Actions)

**Verification**:
- Run `ng test --code-coverage` — shows 80%+ coverage in Angular
- Run `dotnet test /p:CollectCoverage=true` — shows 80%+ coverage in .NET
- Run `ng lint` — no linting errors or warnings
- Run `dotnet format --verify-no-changes` — code passes style checks
- E2E test: Register → Create → Export workflow passes
- CI pipeline fails if coverage < 80%
- Coverage report generated as artifact

---

### Phase 7: Deployment & Production Hardening (Depends on Phase 6)

**Objective**: Deploy to Azure with security, monitoring, and CI/CD automation

**Tasks**:

1. **Azure App Service Deployment**
   - **Publish Profile**:
     - Create publish profile in Visual Studio or Azure Portal
     - Target .NET 10 runtime
     - Enable managed identity (System assigned)
   - **Configuration**:
     - `appsettings.Production.json`:
       - Cosmos DB endpoint (from Key Vault)
       - App Insights key (from Key Vault)
       - JWT signing key (from Key Vault)
       - Disable detailed error messages in production
     - Environment variables in App Service settings:
       - `ASPNETCORE_ENVIRONMENT=Production`
       - `COSMOS_DB_ENDPOINT=https://...`
       - `BLOB_STORAGE_ENDPOINT=https://...`
   - **Managed Identity**:
     - App Service System Assigned Identity
     - Grant access to Cosmos DB, Blob Storage, Key Vault
     - Use `DefaultAzureCredential` in code (no connection strings)
   - **Deployment**:
     - Publish via Azure DevOps or GitHub Actions
     - Run health check: `GET /health` endpoint
     - Verify logs in Application Insights

2. **Azure Functions Deployment**
   - **Publish to Flex Consumption Plan**:
     - From VS Code: Right-click project → Deploy to Function App
     - Or via CLI: `func azure functionapp publish {app-name}`
   - **Configuration**:
     - Managed Identity for Cosmos DB and Blob Storage
     - Key Vault references in function app settings
     - Application Insights connection string
   - **Monitoring**:
     - View execution logs in Azure Portal
     - Monitor duration and invocations
     - Set up alerts for failed executions

3. **Frontend (Angular) Deployment**
   - **Build Production Bundle**:
     - `ng build --configuration production`
     - Output in `dist/` folder
     - Minified, tree-shaken, optimized
   - **Deployment Options**:
     - Option A: Azure Static Web Apps (recommended for SPA)
       - Free tier includes custom domain, auto-deploys from Git
       - Azure Functions API automatically integrated
     - Option B: Azure App Service (served static files from backend)
       - Simpler setup, single deployment unit
     - Option C: Azure Blob Storage + CDN (cheapest)
       - Blob Storage hosts static files
       - CDN for global distribution
   - **Recommended**: Azure Static Web Apps
     - Link GitHub repo, auto-deploy on push
     - Staging environment for pull requests
     - Custom domain + SSL/TLS automatic
   - **Configuration**:
     - `staticwebapp.config.json`:
       ```json
       {
         "routes": [
           {
             "route": "/api/*",
             "redirect": "/index.html"
           }
         ],
         "navigationFallback": {
           "rewrite": "/index.html"
         }
       }
       ```

4. **CI/CD Pipeline** (Azure DevOps or GitHub Actions)
   - **GitHub Actions Example**:
     ```yaml
     name: Build and Deploy
     on:
       push:
         branches: [main]
     
     jobs:
       build-test:
         runs-on: ubuntu-latest
         steps:
           - uses: actions/checkout@v3
           
           # Build Angular
           - uses: actions/setup-node@v3
             with:
               node-version: 20
           - run: npm ci && npm run lint && npm run test:coverage
           - run: ng build --configuration production
           
           # Build .NET
           - uses: actions/setup-dotnet@v3
             with:
               dotnet-version: '10'
           - run: dotnet build
           - run: dotnet test /p:CollectCoverage=true
           
           # Check coverage
           - run: |
               if [ $(grep -oP '(?<=<CoveragePercentage>)\d+' coverage.xml) -lt 80 ]; then
                 exit 1
               fi
       
       deploy:
         needs: build-test
         runs-on: ubuntu-latest
         steps:
           - uses: actions/checkout@v3
           - uses: azure/login@v1
             with:
               creds: ${{ secrets.AZURE_CREDENTIALS }}
           - run: az staticwebapp create...
           - run: func azure functionapp publish...
     ```
   - **Pipeline Stages**:
     1. **Build**: Compile Angular and .NET
     2. **Lint**: Run ESLint and code style checks
     3. **Test**: Run unit tests, measure coverage
     4. **Test Coverage Gate**: Fail if < 80%
     5. **E2E Tests** (optional): Run Cypress tests
     6. **Build Artifacts**: Create deployment packages
     7. **Deploy to Staging**: Deploy to staging environment
     8. **Smoke Tests**: Verify basic functionality
     9. **Deploy to Production**: Promote to production (manual approval)
     10. **Rollback on Failure**: Auto-rollback if tests fail

5. **Security Hardening**
   - **HTTPS/TLS**:
     - Enable HTTPS on App Service (automatic with App Service Domain)
     - Enforce TLS 1.2+: `<httpProtocol><requireSSL>true</requireSSL></httpProtocol>`
   - **CORS Configuration**:
     - In .NET Startup/Program.cs:
       ```csharp
       services.AddCors(options =>
       {
         options.AddPolicy("Production", builder =>
         {
           builder
             .WithOrigins("https://yourdomain.com")
             .AllowAnyMethod()
             .AllowAnyHeader()
             .AllowCredentials();
         });
       });
       ```
   - **Security Headers**:
     - Add middleware for HSTS, CSP, X-Frame-Options:
       ```csharp
       app.UseHsts();
       app.Use((context, next) =>
       {
         context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
         context.Response.Headers.Add("X-Frame-Options", "DENY");
         context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
         return next();
       });
       ```
   - **Rate Limiting**:
     - Implement per-user rate limiting: 100 requests/min
     - Use `Microsoft.AspNetCore.RateLimiting` (built-in .NET 7+)
       ```csharp
       var rateLimitPolicy = "fixed";
       builder.Services.AddRateLimiter(options =>
       {
         options.AddFixedWindowLimiter(rateLimitPolicy, config =>
         {
           config.PermitLimit = 100;
           config.Window = TimeSpan.FromMinutes(1);
         });
       });
       ```
   - **Input Validation**:
     - All endpoints validate input (FluentValidation)
     - No SQL injection (using parameterized Cosmos DB queries)
     - No XSS (Cosmos DB stores text safely, Angular auto-escapes)
   - **Logging & PII**:
     - Never log passwords, full amounts, personal identifiers
     - Hash/mask sensitive data in logs
     - Example: Log `userId=user_***abc` instead of full email

6. **Monitoring & Alerts**
   - **Application Insights Dashboard**:
     - Real-time request rate, exception rate, response time
     - Custom metrics: Expenses created, API endpoint usage
     - Request timeline: Show slow requests
     - Dependency performance: Cosmos DB query times
   - **Alerts**:
     - Exception rate > 5% (immediate alert)
     - Response time > 2s for 5+ requests (warning)
     - Failed logins > 3 in 5 minutes (potential attack)
     - Cosmos DB RU consumption > threshold
     - Function execution time > 30s (timeout risk)
   - **Action Groups**:
     - Email notification to ops team
     - Optional: PagerDuty or Slack integration

7. **Backup & Disaster Recovery**
   - **Cosmos DB**:
     - Enable continuous backup (30-day retention)
     - Point-in-time restore available
     - Geo-redundant replication (multiple regions)
   - **Blob Storage**:
     - RA-GRS (Read-Access Geo-Redundant Storage)
     - Automatic failover to secondary region
   - **Database Backups**:
     - Weekly export to another storage account (cold backup)
     - Retention: 90 days minimum
   - **Disaster Recovery Plan**:
     - RTO (Recovery Time Objective): < 4 hours
     - RPO (Recovery Point Objective): < 1 day
     - Test recovery procedure quarterly

**Verification**:
- Angular build produces optimized dist/ folder
- App Service deploys successfully via CI/CD
- Azure Functions deploy to Flex Consumption Plan
- HTTPS accessible at custom domain
- CI/CD pipeline triggers on push
- Build fails if tests fail or coverage < 80%
- Deployment to staging automatic
- Logs visible in Application Insights
- Alerts configured and tested
- Health check endpoint responds 200
- Backup automated and verified

---

## Relevant Files (To be created)

### Angular 20 Frontend

```
src/
├── app/
│   ├── auth/
│   │   ├── login.component.ts|html|css
│   │   ├── register.component.ts|html|css
│   │   ├── auth.service.ts
│   │   └── auth.guard.ts
│   ├── expenses/
│   │   ├── expense-list.component.ts|html|css
│   │   ├── expense-form.component.ts|html|css
│   │   ├── expense-detail.component.ts|html|css
│   │   └── expense.service.ts
│   ├── categories/
│   │   ├── category-management.component.ts|html|css
│   │   └── category.service.ts
│   ├── reports/
│   │   ├── reporting.component.ts|html|css
│   │   └── chart components
│   ├── shared/
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts
│   │   │   ├── error.interceptor.ts
│   │   │   └── logging.interceptor.ts
│   │   ├── services/
│   │   │   ├── error.service.ts
│   │   │   ├── logging.service.ts
│   │   │   └── expense.store.ts
│   │   ├── components/
│   │   │   ├── error-modal.component.ts|html|css
│   │   │   └── loading-spinner.component.ts|html|css
│   │   └── models/
│   │       ├── expense.model.ts
│   │       ├── category.model.ts
│   │       └── auth-response.model.ts
│   ├── dashboard.component.ts|html|css
│   └── app.component.ts|html|css
├── environments/
│   ├── environment.ts
│   └── environment.prod.ts
├── main.ts
└── index.html

Tests (alongside components):
├── app/auth/auth.service.spec.ts
├── app/expenses/expense.service.spec.ts
├── app/expenses/expense-list.component.spec.ts
├── app/shared/interceptors/auth.interceptor.spec.ts
└── ...

Configuration:
├── angular.json
├── tsconfig.json
├── .eslintrc.json
├── .prettierrc
├── karma.conf.js
└── package.json
```

### .NET 10 Backend (App Service)

```
ExpenseApi/
├── Controllers/
│   ├── AuthController.cs
│   ├── ExpensesController.cs
│   └── CategoriesController.cs
├── Services/
│   ├── AuthService.cs
│   ├── ExpenseService.cs
│   ├── CategoryService.cs
│   └── BlobStorageService.cs
├── Data/
│   ├── Repository/
│   │   ├── IExpenseRepository.cs
│   │   ├── ExpenseRepository.cs
│   │   ├── ICategoryRepository.cs
│   │   └── CategoryRepository.cs
│   └── Models/
│       ├── Expense.cs
│       └── Category.cs
├── Models/
│   ├── Requests/
│   │   ├── CreateExpenseRequest.cs
│   │   ├── UpdateExpenseRequest.cs
│   │   └── LoginRequest.cs
│   ├── Responses/
│   │   ├── ExpenseResponse.cs
│   │   ├── ErrorResponse.cs
│   │   └── AuthResponse.cs
│   └── DTOs/
│       └── (DTO mappings)
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── JwtValidationMiddleware.cs
├── Validation/
│   ├── ExpenseValidator.cs
│   └── CategoryValidator.cs
├── Logging/
│   └── LoggingExtensions.cs
├── Program.cs (Startup configuration, DI, Serilog)
├── appsettings.json
├── appsettings.Production.json
└── ExpenseApi.csproj

Tests/
├── Unit/
│   ├── Services/
│   │   ├── ExpenseServiceTests.cs
│   │   ├── AuthServiceTests.cs
│   │   └── CategoryServiceTests.cs
│   ├── Controllers/
│   │   └── ExpensesControllerTests.cs
│   └── Validation/
│       └── ExpenseValidatorTests.cs
├── Integration/
│   ├── ExpenseRepositoryTests.cs
│   ├── CosmosDbFixture.cs (test setup)
│   └── TestData.cs
└── ExpenseApi.Tests.csproj
```

### Azure Functions (Flex Consumption)

```
ExpenseFunctions/
├── ReceiptProcessorFunction.cs
├── MonthlySummaryFunction.cs
├── Startup.cs (DI registration)
├── local.settings.json
├── host.json
├── ExpenseFunctions.csproj
└── .gitignore
```

### Infrastructure as Code (Optional)

```
Infrastructure/
├── azure-resources.bicep (Cosmos DB, Blob, App Service, Functions)
├── parameters.json
└── deploy.sh
```

### CI/CD

```
.github/workflows/
├── build-and-test.yml (lint, test, coverage gates)
├── deploy-staging.yml (deploy to staging)
└── deploy-production.yml (promote to production)

OR

azure-pipelines.yml (Azure DevOps)
```

---

## Verification Checklist

### Phase 1: Foundation & Authentication
- ✅ Angular 20 dev server runs without errors
- ✅ .NET 10 API builds successfully
- ✅ Cosmos DB container created with userId partition key
- ✅ Register endpoint accepts email/password, creates user
- ✅ Login endpoint returns JWT token
- ✅ Protected endpoint returns 401 without token, 200 with valid token
- ✅ Token refresh works on 401 response
- ✅ Logout clears token in Angular
- ✅ Exceptions logged to Application Insights with correlationId
- ✅ Error modal displays with user-friendly message + copy-to-clipboard Log ID

### Phase 2: Core Data Model & REST API
- ✅ All CRUD endpoints respond with correct HTTP status codes
- ✅ Create endpoint returns 201 Created with location header
- ✅ List endpoint filters by month (default current month)
- ✅ Update endpoint persists changes
- ✅ Delete endpoint soft-deletes expense
- ✅ Validation errors return 400 with field-level messages
- ✅ All exceptions logged to Application Insights
- ✅ Only authenticated users can access API
- ✅ Users can only access their own expenses (403 if accessing another user's data)
- ✅ Monthly summary aggregation works correctly

### Phase 3: Angular UI & Client Logic
- ✅ Login page loads, register form works
- ✅ Successful login stores token and redirects to dashboard
- ✅ Expense grid displays with current month selected
- ✅ Month/year selector changes displayed data
- ✅ Add button opens form, submission creates expense
- ✅ Edit button opens form with pre-populated data
- ✅ Update persists changes
- ✅ Delete button shows confirmation, soft delete works
- ✅ Form validation prevents invalid submission (error messages inline)
- ✅ All API errors show friendly message + Log ID
- ✅ Copy error button adds error + Log ID to clipboard
- ✅ Logout clears token and redirects to login
- ✅ Layout responsive on mobile and desktop

### Phase 4: Advanced Features
- ✅ Receipt upload stores in Blob Storage
- ✅ Thumbnail displays in expense grid
- ✅ Delete receipt removes from blob and Expense document
- ✅ Create custom category in category management
- ✅ Filter expenses by category works
- ✅ Export button downloads Excel file
- ✅ Open exported file, verify formatting and data correct
- ✅ Charts load with correct monthly data
- ✅ Date range picker filters charts
- ✅ Search box finds expenses by description
- ✅ Filter badges display active filters
- ✅ Clear filters resets to default view

### Phase 5: Background Jobs & Async Processing
- ✅ Upload receipt, Function processes asynchronously
- ✅ Expense document updated with receiptUrl
- ✅ Function execution logged in Application Insights
- ✅ Monthly summary generated on schedule
- ✅ Summary data accessible in Cosmos DB
- ✅ Function errors logged with context

### Phase 6: Testing & Code Quality
- ✅ `ng test --code-coverage` shows 80%+ coverage
- ✅ `dotnet test /p:CollectCoverage=true` shows 80%+ coverage
- ✅ `ng lint` shows no errors or warnings
- ✅ `dotnet format --verify-no-changes` passes
- ✅ E2E test: Register → Create → Export workflow passes
- ✅ CI pipeline fails if coverage < 80%
- ✅ Coverage report generated as artifact

### Phase 7: Deployment & Production Hardening
- ✅ Angular production build generates optimized dist/ folder
- ✅ App Service deploys successfully via CI/CD
- ✅ Functions deploy to Flex Consumption Plan
- ✅ App accessible via HTTPS at custom domain
- ✅ CI/CD pipeline triggers on push to main branch
- ✅ Build fails if tests fail or linting errors
- ✅ Deployment to staging automatic after successful build
- ✅ Health check endpoint responds 200 OK
- ✅ Logs stream to Application Insights
- ✅ Alerts configured (exception rate, response time, failed logins)
- ✅ Alerts tested and notifications sent
- ✅ Backup automated and verified
- ✅ HSTS and CSP headers configured
- ✅ Rate limiting enforced
- ✅ CORS restricted to production domain

---

## Key Decisions & Rationale

1. **Angular 20 + .NET 10**: Latest stable LTS versions with long-term support (until 2028 for .NET)
2. **App Service for REST API**: Zero cold starts, best for frequent CRUD operations
3. **Azure Functions for background jobs**: Cost-effective, event-driven, async processing
4. **Cosmos DB partition by userId**: Enables future multi-user/team expansion without schema changes
5. **Serilog + Application Insights**: Production-grade structured logging, centralized monitoring dashboard
6. **JWT authentication**: Simple, self-contained, no external dependency for single-user app
7. **Managed Identity**: Secure credential management, no connection strings in code
8. **Hybrid serverless approach**: Best of both worlds—sync API (App Service) + async jobs (Functions)
9. **80%+ code coverage**: Balances test investment with code quality
10. **CI/CD with coverage gates**: Prevents regression, enforces quality standards

---

## Future Enhancements (Post-MVP)

1. **Multi-user expansion**: Add team/organization support (userId partition key already supports this)
2. **API Management**: Add for versioning, rate limiting per API key, developer portal (when scaling)
3. **Two-factor authentication**: Email OTP or authenticator apps for enhanced security
4. **Offline support**: Service Workers + IndexedDB for offline expense creation
5. **Receipt OCR**: Computer Vision API integration to auto-extract amount and date from receipts
6. **Mobile app**: React Native or Flutter for iOS/Android
7. **Advanced analytics**: Spending forecasts, budget alerts, category recommendations
8. **Recurring expenses**: Auto-create monthly recurring items
9. **Approval workflows**: Manager approval for team expense reimbursement
10. **Cost optimization**: Monitor and optimize Cosmos DB RU consumption

---

## Estimated Timeline (Per Developer)

- **Phase 1** (Foundation): 2-3 days
- **Phase 2** (CRUD API): 3-4 days
- **Phase 3** (UI): 4-5 days
- **Phase 4** (Advanced features): 3-4 days
- **Phase 5** (Background jobs): 2-3 days
- **Phase 6** (Testing): 3-4 days
- **Phase 7** (Deployment): 1-2 days
- **Total**: 18-25 days for full implementation

---

## Estimated Monthly Cost (Azure)

- App Service (B1): $80-100
- Azure Functions (Flex): $0-50 (pay-per-execution)
- Cosmos DB (auto-scale): $24-100+
- Blob Storage (receipts): $5-20
- Application Insights: $0 (free tier 5GB/month)
- Static Web Apps (frontend): $0-12
- **Total**: $110-280/month

---

## Tech Stack Summary

| Component | Technology | Version | Rationale |
|-----------|-------------|---------|-----------|
| Frontend | Angular | 20 | Latest stable, Signal-based forms, excellent testing support |
| Backend API | .NET Core | 10 | LTS until 2028, C# 14, superior performance |
| Database | Azure Cosmos DB | SQL API | Serverless, global distribution, audit-ready |
| Background Jobs | Azure Functions | Flex Consumption | Pay-per-execution, no cold starts with Flex plan |
| Storage | Azure Blob | RA-GRS | Cost-effective receipt storage with redundancy |
| Logging | Serilog + App Insights | Latest | Production-grade structured logging |
| Testing | Jasmine/xUnit | Latest | Industry standard, excellent tooling |
| Hosting | App Service + Functions | Standard/Flex | Clear separation: sync API + async jobs |

---

## References & Resources

- Angular 20 Documentation: https://angular.io/docs
- .NET 10 Documentation: https://learn.microsoft.com/dotnet
- Azure Cosmos DB: https://learn.microsoft.com/azure/cosmos-db
- Serilog: https://serilog.net/
- xUnit: https://xunit.net/
- Application Insights: https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview
```

Copy the above markdown into your `ExpensesProjectSpecs.md` file, and you'll have a complete reference document you can work from whenever you're ready to start implementing. The plan includes detailed task breakdowns, verification steps, relevant files, and cost estimates—everything you need to execute confidently later!

Similar code found with 1 license type