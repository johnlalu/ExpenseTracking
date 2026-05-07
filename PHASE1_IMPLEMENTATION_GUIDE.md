# Phase 1 Implementation Guide

## Completed Infrastructure Setup

This guide documents the completed Phase 1 foundational setup for the Expense Reimbursement Tracking application.

### Angular 20 Frontend - Completed

#### Project Structure
- ✅ Created feature modules: `auth`, `expenses`, `categories`, `reports`
- ✅ Created shared infrastructure: `models`, `services`, `interceptors`
- ✅ Configured Material Design theme (Indigo-Pink)
- ✅ Added Google Fonts and Material Icons

#### Services Implemented
1. **AuthService** (`shared/services/auth.service.ts`)
   - User login/registration
   - Token management (access & refresh tokens)
   - JWT decoding and validation
   - Automatic token refresh on expiration

2. **ExpenseService** (`shared/services/expense.service.ts`)
   - CRUD operations for expenses
   - Monthly summary retrieval
   - Export functionality

3. **CategoryService** (`shared/services/category.service.ts`)
   - Retrieve all categories
   - Create custom categories
   - Delete categories

4. **ErrorService** (`shared/services/error.service.ts`)
   - Centralized error management
   - Error state broadcasting

#### HTTP Interceptors
1. **AuthInterceptor** (`shared/interceptors/auth.interceptor.ts`)
   - Automatically attaches JWT token to requests
   - Handles token refresh on 401 responses

2. **ErrorInterceptor** (`shared/interceptors/error.interceptor.ts`)
   - Catches and transforms HTTP errors
   - Emits errors via ErrorService

#### Guards
1. **AuthGuard** (`shared/services/auth.guard.ts`)
   - Protects authenticated routes
   - Redirects to login if not authenticated

#### Models (Type Safety)
- `auth.model.ts`: AuthResponse, LoginRequest, RegisterRequest, JwtPayload
- `expense.model.ts`: Expense, CreateExpenseRequest, ExpenseListResponse, MonthlySummary
- `category.model.ts`: Category, DEFAULT_CATEGORIES
- `error.model.ts`: ErrorResponse, AppError

#### Routing Configuration
- Login/Register routes (public)
- Dashboard route (authenticated)
- Expenses CRUD routes (authenticated)
- Reports route (authenticated)
- Categories management route (authenticated)
- Automatic redirect to dashboard on invalid routes

---

### .NET 10 Web API - Completed

#### Project Structure Created
```
ExpenseApi/
├── Models/
│   ├── Expense.cs
│   ├── Category.cs
│   ├── Requests/
│   │   ├── CreateExpenseRequest.cs
│   │   ├── UpdateExpenseRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── RegisterRequest.cs
│   └── Responses/
│       ├── AuthResponse.cs
│       ├── ErrorResponse.cs
│       └── ExpenseResponse.cs
├── Controllers/
│   └── BaseController.cs
├── Services/ (ready for implementation)
├── Data/Repository/ (interfaces created)
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Validation/
│   ├── CreateExpenseValidator.cs
│   ├── UpdateExpenseValidator.cs
│   ├── RegisterRequestValidator.cs
│   └── LoginRequestValidator.cs
├── Logging/
│   └── LoggingExtensions.cs
└── AppConfig.cs
```

#### Middleware Implemented
1. **ExceptionHandlingMiddleware**
   - Global exception handling
   - Correlation ID tracking
   - Standardized error responses
   - Handles common exceptions (Unauthorized, NotFound, BadRequest)

#### Validation (FluentValidation)
1. **CreateExpenseValidator**
   - Description: Required, max 500 chars
   - Amount: > 0, <= 999,999.99
   - Currency: 3-letter code
   - Category: Required, max 100 chars
   - PurchaseDate: Required, not in future

2. **UpdateExpenseValidator**
   - Same rules as CreateExpenseValidator

3. **RegisterRequestValidator**
   - Email: Required, valid format
   - Password: Min 8 chars, uppercase, lowercase, digit, special char
   - ConfirmPassword: Must match Password

4. **LoginRequestValidator**
   - Email: Required, valid format
   - Password: Required

#### Logging Configuration
- Serilog with Application Insights sink
- Machine name and thread ID enrichment
- Expense operation logging helpers
- Console and cloud logging

#### Domain Models
1. **Expense.cs**
   - 11 properties with XML documentation
   - Includes soft delete support (IsDeleted)
   - Timestamps for audit (CreatedAt, UpdatedAt)

2. **Category.cs**
   - Basic model with soft delete support
   - User-specific and default categories support

#### DTOs (Request/Response)
1. **Requests**
   - CreateExpenseRequest
   - UpdateExpenseRequest
   - LoginRequest
   - RegisterRequest

2. **Responses**
   - AuthResponse (with access/refresh tokens)
   - ErrorResponse (with validation details)
   - ExpenseResponse (for API responses)

#### Configuration
1. **AppConfig.cs**
   - JwtSettings (secret key, issuer, audience, expiration)
   - CosmosDbSettings (endpoint, key, database, container)
   - DefaultCategories list

2. **NuGet Packages Added**
   - Microsoft.Azure.Cosmos 3.58.0
   - Serilog 4.2.1 + AspNetCore 9.1.0
   - Serilog.Sinks.ApplicationInsights 4.1.1
   - Microsoft.ApplicationInsights.AspNetCore 2.22.5
   - FluentValidation 11.11.0
   - AutoMapper 13.0.1

#### Repository Interfaces (Ready for Implementation)
1. **IExpenseRepository**
   - CreateAsync, GetByIdAsync, GetByMonthYearAsync
   - GetByDateRangeAsync, GetByCategoryAsync
   - UpdateAsync, DeleteAsync
   - GetMonthlySummaryAsync

2. **ICategoryRepository**
   - GetAllAsync, GetByIdAsync, CreateAsync, DeleteAsync

---

## Next Steps for Phase 1 Implementation

### Backend (.NET)

#### 1. Implement AuthService
- **File**: `Services/AuthService.cs`
- **Implement**:
  - `RegisterAsync()`: Hash password with bcrypt, create user in Cosmos DB
  - `LoginAsync()`: Verify password, generate JWT tokens
  - `RefreshTokenAsync()`: Validate refresh token, issue new access token
  - `ValidateTokenAsync()`: Verify JWT signature and claims

#### 2. Implement Repository Pattern
- **Files**: `Data/Repository/ExpenseRepository.cs`, `Data/Repository/CategoryRepository.cs`
- **Implement**: CRUD methods with Cosmos DB connectivity using partition key `/userId`

#### 3. Configure Program.cs
- Set up Serilog logging
- Register DI containers (services, repositories, validators)
- Add middleware pipeline (exception handling, authentication)
- Configure CORS for Angular frontend
- Configure JWT authentication

#### 4. Create API Controllers
- **AuthController**: `/api/auth` (register, login, refresh)
- **ExpensesController**: `/api/expenses` (CRUD, monthly summary)
- **CategoriesController**: `/api/categories` (list, create, delete)

### Frontend (Angular)

#### 1. Create Authentication Components
- **LoginComponent**: Email/password form
- **RegisterComponent**: Registration form with password confirmation

#### 2. Create Expense Management Components
- **ExpenseDashboardComponent**: Main dashboard with summary
- **ExpenseListComponent**: Paginated list with filters
- **ExpenseFormComponent**: Add/edit expense with Material form
- **ExpenseDetailComponent**: Single expense view

#### 3. Create Additional Components
- **ReportViewComponent**: Monthly/category summaries with charts
- **CategoryListComponent**: Manage custom categories
- **NavbarComponent**: Top navigation with user menu
- **ErrorSnackbarComponent**: Error display notifications

#### 4. Update App Layout
- Create main layout component with sidebar
- Add responsive navigation
- Implement error handling display

---

## Configuration Files to Update

### .NET (Program.cs)
```csharp
// Add to Program.cs:
builder.Services.ConfigureSerilog(builder.Configuration);
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configure */ });

// CORS for Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
```

### appsettings.json
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights" }
    ]
  },
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR_KEY"
  },
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY",
    "Issuer": "ExpenseApi",
    "Audience": "ExpenseApp",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "CosmosDb": {
    "EndpointUri": "YOUR_COSMOS_URI",
    "PrimaryKey": "YOUR_KEY",
    "DatabaseName": "ExpenseDb",
    "ContainerName": "Expenses"
  }
}
```

### Angular (environment files)
Create `environment.ts` and `environment.prod.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

---

## Running the Applications

### Angular Frontend
```powershell
cd expense-frontend
npm start
# Opens on http://localhost:4200
```

### .NET Backend
```powershell
cd ExpenseApi
dotnet run
# Runs on http://localhost:5000
```

---

## Testing Strategy

### Backend Unit Tests
- Test each validator with valid/invalid inputs
- Test AuthService token generation and validation
- Test repository CRUD operations with mocked Cosmos DB

### Backend Integration Tests
- Test full API endpoints with real database
- Test error handling middleware
- Test authentication flow

### Frontend Unit Tests
- Test services with mocked HTTP calls
- Test component logic and state management
- Test guards and interceptors

### E2E Tests
- Test complete user flows (login → expense creation → report viewing)

---

## Deployment Considerations

### Azure Resources Required
1. App Service (ASP.NET Core hosted)
2. Azure Cosmos DB (SQL API)
3. Storage Account (for receipt uploads)
4. Application Insights (monitoring)
5. Azure AD B2C (optional, for enterprise auth)
6. Azure Functions (optional, for background jobs)

### Environment Configuration
- Use Azure Key Vault for secrets
- Use managed identities for service-to-service communication
- Configure CORS for deployed frontend domain

