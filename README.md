# PennyWise

PennyWise is an ASP.NET Core Razor Pages budgeting app. It uses EF Core with SQLite by default, supports signup/login, and lets users track transactions, budgets, custom categories, recurring activity, savings goals, overview metrics, and analytics.

## Tech Stack

- ASP.NET Core Razor Pages
- .NET 10 (`net10.0`)
- Entity Framework Core
- PdfPig for text-based PDF statement parsing
- SQLite by default
- PostgreSQL support through configuration
- Static assets served from `wwwroot`

## Project Structure

```text
PennyWise/
|-- Data/                 # EF Core DbContext, seed data, and database entities
|-- Migrations/           # EF Core migrations
|-- Models/               # View models used by Razor pages
|-- Pages/                # Razor pages and page models
|   |-- Auth/             # Login and signup
|   |-- Dashboard/        # Overview, transactions, budgets, categories, recurring, goals, settings, analytics
|   `-- Shared/           # Layout and shared partials
|-- wwwroot/              # CSS, JavaScript, and static files
|-- Program.cs            # Application startup, auth, routes, DB wiring
|-- appsettings.json      # Database provider and connection strings
`-- PennyWise.csproj      # Project definition and NuGet packages
```

## Main Features

- Email/password signup and login with cookie auth
- SQLite database by default, with PostgreSQL configuration support
- User-specific transactions, budgets, categories, recurring rules, settings, and savings goals
- Manual transaction entry with edit/delete support
- Text-based bank PDF and CSV import with a review screen before saving
- Import summary for detected rows, skipped rows, possible duplicates, rows needing review, and recurring guesses
- Editable import preview rows, including date, description, category, type, amount, and selected/unselected state
- Recurring transaction suggestions that require user approval before rules are created
- Category management for custom categories such as rent, subscriptions, pets, travel, school, and debt
- Monthly budget tracking with remaining/over-budget alerts
- Savings goals with progress tracking
- Transactions filters, sorting, month navigation, and pagination
- Clickable dashboard and analytics charts that drill into filtered transaction views
- Helpful empty states for new users
- Light/dark theme toggle

## Prerequisites

Install these before running the app:

- .NET SDK 10.0 or a compatible SDK that supports `net10.0`
- Chocolatey, used here to install the SQLite command-line tool
- SQLite CLI, so `sqlite3 pennywise.db ...` works in PowerShell

Check .NET:

```powershell
dotnet --version
```

## Install Chocolatey

Open **PowerShell as Administrator**.

If this command returns `Restricted`:

```powershell
Get-ExecutionPolicy
```

Run this first:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
```

Then install Chocolatey:

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iwr https://community.chocolatey.org/install.ps1 -UseBasicParsing | iex
```

Close and reopen PowerShell, then verify:

```powershell
choco --version
```

Official Chocolatey install docs:

```text
https://chocolatey.org/install
```

## Install SQLite CLI

Open **PowerShell as Administrator** and run:

```powershell
choco install sqlite -y
```

Close and reopen PowerShell, then verify:

```powershell
sqlite3 -version
```

If `sqlite3` is still not recognized, restart your terminal or your computer so PATH refreshes.

## Run Locally

From the project folder:

```powershell
cd C:\Users\elina\OneDrive\Documents\GitHub\PennyWise
dotnet restore
dotnet run
```

Open the URL shown in the terminal. This project usually uses:

```text
http://localhost:5005
https://localhost:7255
```

The app applies EF Core migrations and seeds demo data on startup.

## Demo Login

You can create your own account from the sign-up page, or use the seeded demo account:

```text
Email: demo@pennywise.local
Password: PennyWise123!
```

Opening `/` or `/login` always shows the login page first. If an old login cookie exists, the login page clears it so the app starts from a fresh sign-in flow. Direct dashboard URLs are still protected and redirect to login when there is no valid session.

## Database Location

The default SQLite database file is:

```text
C:\Users\elina\OneDrive\Documents\GitHub\PennyWise\pennywise.db
```

SQLite may also create these helper files while the app is running:

```text
pennywise.db-shm
pennywise.db-wal
```

Do not delete the `-shm` or `-wal` files while the app is running. They are normal SQLite working files.

## Database Configuration

The database provider and connection strings are in `appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "Sqlite": "Data Source=pennywise.db",
    "Postgres": "Host=localhost;Port=5432;Database=pennywise;Username=postgres;Password=pennywise"
  }
}
```

The app reads this in `Program.cs` and configures EF Core:

- `DatabaseProvider = Sqlite` uses `pennywise.db`
- `DatabaseProvider = Postgres` uses the `Postgres` connection string

## What Is Stored In The DB

PennyWise stores these main app tables:

```text
Users
Categories
Transactions
Budgets
RecurringTransactions
SavingsGoals
```

EF Core also creates migration/system tables:

```text
__EFMigrationsHistory
__EFMigrationsLock
sqlite_sequence
```

### Users

Created when someone signs up.

Stores:

- `Id`
- `Email`
- `FullName`
- `PasswordHash`
- `Currency`
- `MonthlySavingsGoal`
- `CreatedAt`

Passwords are not stored as plain text. The app stores a password hash.

### Categories

Seeded by the app, and users can add their own categories.

Stores:

- `Id`
- `UserId`
- `Name`
- `Icon`
- `Color`
- `IsDefault`

Current seeded categories:

```text
Food & Dining
Transportation
Shopping
Utilities
Entertainment
Health
Income
```

Custom category examples:

```text
Rent
Subscriptions
Pets
Travel
School
Debt
```

Default categories have `UserId = null` and `IsDefault = 1`. Custom categories belong to one user.

### Transactions

Created when the user adds a transaction, imports a bank PDF/CSV file, or generates recurring activity.

Stores:

- `Id`
- `UserId`
- `CategoryId`
- `Amount`
- `Description`
- `Type`
- `Date`
- `CreatedAt`

Transaction type values:

```text
0 = Expense
1 = Income
```

### Budgets

Created or updated when the user saves a budget on the Budgets page.

Stores:

- `Id`
- `UserId`
- `CategoryId`
- `MonthlyLimit`
- `Month`
- `Year`
- `CreatedAt`

There can only be one budget per user/category/month/year.

### RecurringTransactions

Created from the Recurring page.

Stores:

- `Id`
- `UserId`
- `CategoryId`
- `Amount`
- `Description`
- `Type`
- `DayOfMonth`
- `IsActive`
- `LastGeneratedYear`
- `LastGeneratedMonth`
- `CreatedAt`

Recurring rules do not automatically insert transactions in the background. The user clicks **Generate due items**, and the app creates this month's expected transactions for active rules that have not already been generated.

### SavingsGoals

Created from the Goals page.

Stores:

- `Id`
- `UserId`
- `Name`
- `TargetAmount`
- `CurrentAmount`
- `CreatedAt`

## How Data Is Collected

Data is collected through the app UI, bank statement imports, and recurring rules.

1. The user signs up.
   A row is inserted into `Users`.

2. The user logs in.
   The app checks `Users.Email` and `Users.PasswordHash`, then creates an auth cookie.

3. The user updates settings.
   The Settings page updates `Users.FullName`, `Users.Email`, `Users.Currency`, `Users.MonthlySavingsGoal`, or `Users.PasswordHash`.

4. The user creates categories.
   The Categories page inserts custom rows into `Categories` with the current user's `UserId`.

5. The user adds a transaction.
   The Transactions page inserts a row into `Transactions`.

6. The user imports a bank statement or transaction export.
   The Transactions page reads text-based PDF statements and CSV exports, then shows an editable review screen. The user can change categories, types, descriptions, dates, amounts, and selected rows before saving. Only selected rows are inserted into `Transactions`. If a reviewed row uses a category name that does not exist yet, the app creates a custom category first.

7. The user saves a budget.
   The Budgets page inserts or updates a row in `Budgets`.

8. The user creates recurring rules.
   The Recurring page inserts rows into `RecurringTransactions`. Imports can also suggest recurring rules, but the user must approve them on the import review screen before they are saved. Clicking **Generate due items** creates matching rows in `Transactions`.

9. The user creates savings goals.
   The Goals page inserts rows into `SavingsGoals`.

10. Dashboard pages query the DB.
    Overview, Transactions, Budgets, Categories, Recurring, Goals, Settings, and Analytics read from the database and calculate what the user sees.

There is no live bank connection. Bank data gets into the app when the user imports a bank PDF/CSV file or types transactions manually.

## Bank Statement Import

The Transactions page can import:

- Text-based PDF bank statements
- CSV transaction exports downloaded from a bank or credit card account

Import flow:

1. Upload a bank PDF or CSV on the Transactions page.
2. PennyWise parses the file and opens a review screen.
3. Review the import summary:
   - transactions detected
   - rows skipped
   - possible duplicates
   - rows needing review
   - recurring guesses
4. Edit any detected transaction before saving:
   - selected/unselected
   - date
   - description
   - category
   - income/expense type
   - amount
5. Review recurring guesses, such as monthly rent, Netflix, phone bills, or paychecks.
6. Click **Import selected** to save approved transactions and approved recurring rules.

Nothing from a bank statement is saved until **Import selected** is clicked.

PDF limits:

- Text-based PDFs work best.
- Scanned image PDFs do not work yet because they need OCR.
- Every bank formats PDFs differently, so imported transactions should be reviewed after upload.

For CSV imports, PennyWise looks for common bank export headers:

```text
Date, Posted Date, Transaction Date, Posting Date
Description, Merchant, Payee, Memo, Details, Narrative
Amount
Debit, Withdrawal, Outflow
Credit, Deposit, Inflow
Category
Type, Transaction Type, Debit/Credit
```

The strict PennyWise CSV format is also supported:

```text
Date,Description,Category,Type,Amount
```

Example:

```csv
Date,Description,Category,Type,Amount
2026-04-01,Paycheck,Income,Income,2500.00
2026-04-03,Rent,Rent,Expense,1400.00
2026-04-05,Netflix,Subscriptions,Expense,15.49
```

Accepted transaction types include `Income`, `Expense`, `Credit`, `Debit`, `Deposit`, `Withdrawal`, `Purchase`, `Fee`, and `Charge`.

Import behavior:

- Negative amounts are imported as expenses.
- Positive signed amounts are imported as income unless the CSV type/debit column says otherwise.
- Separate `Debit` and `Credit` columns are supported.
- If the import has no category column, PennyWise guesses a category from the description.
- Possible duplicate rows with the same date, description, amount, and type are marked in the review screen and unselected by default.
- Rows PennyWise is unsure about are marked for review.
- Repeated transactions with the same description, amount, type, and similar day of month across at least two months are shown as recurring guesses.
- Recurring guesses are saved only when the user approves them.

Example bank-style CSV with separate debit and credit columns:

```csv
Transaction Date,Description,Debit,Credit
2026-04-01,Payroll Deposit,,2500.00
2026-04-03,Apartment Rent,1400.00,
2026-04-05,Netflix,15.49,
```

## Transactions Page

The Transactions page supports:

- manual transaction entry
- edit/delete for existing transactions
- text-based PDF and CSV import
- month switching with **Prev** and **Next**
- filters by keyword, category, type, and date range
- sort dropdown:
  - newest
  - oldest
  - highest amount
  - lowest amount
  - A-Z description
- pagination with 10, 25, or 50 rows per page
- total result count and previous/next page controls

If no custom date range is selected, Transactions shows the selected month. If a `From` or `To` date is entered, the custom date range takes priority.

## Dashboard Drill-Down

Overview and Analytics charts are interactive:

- Click an income chart point to open Transactions filtered to that month and income.
- Click an expense chart point to open Transactions filtered to that month and expenses.
- Click a savings chart point to open Transactions for that month.
- Click a spending category donut slice or category label to open Transactions filtered to that category and month.
- On Budgets, click **View spending** to open Transactions filtered to that budget category and month.

## Budget Alerts

Budgets show practical status messages:

- remaining amount when the budget is healthy
- warning when usage reaches 80% or more
- over-budget amount when spending passes the limit

Examples:

```text
You have $120.00 left for Groceries this month.
You used 84% of your Food & Dining budget.
You are over budget by $42.00.
```

## Empty States

New users should see helpful prompts instead of confusing blank sections. Empty states point users toward the next useful action:

- add the first transaction
- upload a statement
- create the first budget
- add a recurring rule
- set a savings goal

## Inspect The Database

From the project folder:

```powershell
cd C:\Users\elina\OneDrive\Documents\GitHub\PennyWise
```

Show tables:

```powershell
sqlite3 pennywise.db ".tables"
```

Show users without password hashes:

```powershell
sqlite3 pennywise.db "select Id, Email, FullName, CreatedAt from Users;"
```

Show categories:

```powershell
sqlite3 pennywise.db "select Id, UserId, Name, Icon, Color, IsDefault from Categories order by IsDefault desc, Name;"
```

Show raw transactions:

```powershell
sqlite3 pennywise.db "select Id, UserId, CategoryId, Amount, Description, Type, Date from Transactions order by Date desc;"
```

Show raw budgets:

```powershell
sqlite3 pennywise.db "select * from Budgets;"
```

Show recurring rules:

```powershell
sqlite3 pennywise.db "select Id, UserId, CategoryId, Amount, Description, Type, DayOfMonth, IsActive, LastGeneratedYear, LastGeneratedMonth from RecurringTransactions;"
```

Show savings goals:

```powershell
sqlite3 pennywise.db "select Id, UserId, Name, CurrentAmount, TargetAmount, CreatedAt from SavingsGoals;"
```

Show profile settings:

```powershell
sqlite3 pennywise.db "select Id, Email, FullName, Currency, MonthlySavingsGoal, CreatedAt from Users;"
```

Show transactions with user email and category name:

```powershell
sqlite3 pennywise.db "select t.Id, u.Email, c.Name as Category, t.Amount, case t.Type when 0 then 'Expense' when 1 then 'Income' end as Type, t.Description, t.Date from Transactions t join Users u on u.Id = t.UserId join Categories c on c.Id = t.CategoryId order by t.Date desc;"
```

Show budgets with category names:

```powershell
sqlite3 pennywise.db "select b.Id, u.Email, c.Name as Category, b.MonthlyLimit, b.Month, b.Year from Budgets b join Users u on u.Id = b.UserId join Categories c on c.Id = b.CategoryId order by b.Year desc, b.Month desc, c.Name;"
```

Show monthly income and expenses:

```powershell
sqlite3 pennywise.db "select strftime('%Y-%m', Date) as Month, case Type when 0 then 'Expense' when 1 then 'Income' end as Type, sum(Amount) as Total from Transactions group by Month, Type order by Month desc, Type;"
```

## Build And Test

Build:

```powershell
dotnet build
```

If the app is already running, Windows may lock files in `bin/Debug/...`. Stop the running app or build to a temporary output folder:

```powershell
dotnet build --no-restore -p:UseAppHost=false -o .\temp-build
```

Run tests if tests are added later:

```powershell
dotnet test
```

## Troubleshooting

### `sqlite3` is not recognized

Install SQLite CLI:

```powershell
choco install sqlite -y
```

Then reopen PowerShell and run:

```powershell
sqlite3 -version
```

### Chocolatey says admin access is required

Close PowerShell, reopen it with **Run as Administrator**, then rerun the command.

### The database looks empty

Start the app once:

```powershell
dotnet run
```

The app applies migrations and seeds the demo data during startup.

### Build fails because `PennyWise.exe` or `PennyWise.dll` is locked

The app is already running. Stop the running app, or build to `temp-build`:

```powershell
dotnet build --no-restore -p:UseAppHost=false -o .\temp-build
```

### SQLite database is locked

Close the running app or any DB browser using `pennywise.db`, then retry the command.

## Notes

- In development, detailed errors are enabled.
- In non-development environments, the app uses exception handling and HSTS middleware.
- Razor Pages and static assets are mapped in `Program.cs`.
- Dashboard pages require login.
