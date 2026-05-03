# PennyWise

PennyWise is an ASP.NET Core Razor Pages budgeting app. It uses EF Core with SQLite by default, supports signup/login, and lets users track transactions, budgets, custom categories, recurring activity, savings goals, and analytics.

## Tech Stack

- ASP.NET Core Razor Pages on .NET 10 (`net10.0`)
- Entity Framework Core (SQLite by default, PostgreSQL supported via configuration)
- PdfPig for text-based PDF statement parsing
- Static assets served from `wwwroot`

## Project Structure

```text
PennyWise/
|-- Data/                 # EF Core DbContext, seed data, entities
|-- Migrations/           # EF Core migrations
|-- Models/               # View models
|-- Pages/                # Razor pages (Auth, Dashboard, Shared)
|-- wwwroot/              # CSS, JS, static files
|-- Program.cs            # Startup, auth, routes, DB wiring
|-- appsettings.json      # Database provider and connection strings
`-- PennyWise.csproj      # Project definition and packages
```

## Features

- Email/password signup and login with cookie auth
- Manual transaction entry with edit/delete
- Text-based PDF and CSV bank statement import with editable review screen
- Duplicate detection, "needs review" flagging, and recurring rule suggestions during import
- Monthly budgets with remaining/over-budget alerts (warns at 80% usage)
- Custom categories (rent, subscriptions, pets, travel, school, debt, etc.)
- Recurring transaction rules (user clicks **Generate due items** to create the month's rows)
- Savings goals with progress tracking
- Filters, sorting, month navigation, and pagination on transactions
- Clickable Overview/Analytics charts that drill into filtered transaction views
- Helpful empty states for new users
- Light/dark theme toggle

## Prerequisites

- .NET SDK 10.0 (or compatible `net10.0` SDK)
- SQLite CLI (optional, only needed to inspect the DB by hand)

On Windows, install the SQLite CLI via Chocolatey from an Administrator PowerShell:

```powershell
choco install sqlite -y
```

See <https://chocolatey.org/install> if Chocolatey itself is not installed.

## Run Locally

```powershell
dotnet restore
dotnet run
```

Open the URL printed in the terminal (typically `http://localhost:5005` or `https://localhost:7255`). EF Core migrations and demo data are applied on startup.

## Demo Login

Sign up from the UI, or use the seeded account:

```text
Email:    demo@pennywise.local
Password: PennyWise123!
```

`/` and `/login` always show the login page first and clear stale cookies. Direct dashboard URLs are protected and redirect to login when there is no valid session.

## Database

Default SQLite file: `pennywise.db` in the project folder. SQLite may also create `pennywise.db-shm` and `pennywise.db-wal` while the app is running — leave them alone.

Provider and connection strings live in `appsettings.json`:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "Sqlite": "Data Source=pennywise.db",
    "Postgres": "Host=localhost;Port=5432;Database=pennywise;Username=postgres;Password=pennywise"
  }
}
```

Set `DatabaseProvider` to `Sqlite` or `Postgres`. `Program.cs` reads this and configures EF Core accordingly.

### Tables

| Table                   | Purpose                                                     |
| ----------------------- | ----------------------------------------------------------- |
| `Users`                 | Account, hashed password, currency, monthly savings goal    |
| `Categories`            | Seeded defaults plus per-user custom categories             |
| `Transactions`          | Manual entries, imported rows, generated recurring activity |
| `Budgets`               | One monthly limit per user/category/month/year              |
| `RecurringTransactions` | Rules generated on demand into `Transactions`               |
| `SavingsGoals`          | Goal name, target, current amount                           |

`Transactions.Type` uses `0 = Expense`, `1 = Income`. Default categories have `UserId = NULL` and `IsDefault = 1`. Passwords are stored as a hash, never plain text.

Seeded categories: Food & Dining, Transportation, Shopping, Utilities, Entertainment, Health, Income.

## Bank Statement Import

The Transactions page imports text-based PDFs and CSV exports. Imports open a review screen showing detected rows, skipped rows, possible duplicates, rows needing review, and recurring guesses. You can edit date, description, category, type, amount, and selected state before saving. Nothing is saved until you click **Import selected**. Scanned (image-only) PDFs are not supported.

CSV headers PennyWise recognizes:

```text
Date | Posted Date | Transaction Date | Posting Date
Description | Merchant | Payee | Memo | Details | Narrative
Amount | Debit | Withdrawal | Outflow | Credit | Deposit | Inflow
Category
Type | Transaction Type | Debit/Credit
```

Strict PennyWise CSV format:

```csv
Date,Description,Category,Type,Amount
2026-04-01,Paycheck,Income,Income,2500.00
2026-04-03,Rent,Rent,Expense,1400.00
2026-04-05,Netflix,Subscriptions,Expense,15.49
```

Accepted type values: `Income`, `Expense`, `Credit`, `Debit`, `Deposit`, `Withdrawal`, `Purchase`, `Fee`, `Charge`. Negative amounts are imported as expenses; positive amounts are income unless a type/debit column says otherwise. If no category column is present, the app guesses one from the description. Possible duplicates and uncertain rows are flagged and unselected by default. Recurring guesses (same description, amount, type, similar day-of-month across at least two months) are saved only if you approve them.

## Transactions Page

Supports manual entry, edit/delete, PDF/CSV import, month switching (Prev/Next), filters (keyword, category, type, date range), sort (newest, oldest, highest/lowest amount, A–Z), and pagination (10/25/50 rows per page). A custom `From`/`To` date range overrides the selected month.

## Dashboard Drill-Down

Clicking chart points opens Transactions filtered to the matching month, type, or category. From a budget row, **View spending** opens Transactions filtered to that category and month.

## Inspect the Database

A few examples (run from the project folder):

```powershell
sqlite3 pennywise.db ".tables"
sqlite3 pennywise.db "select Id, Email, FullName, CreatedAt from Users;"
sqlite3 pennywise.db "select t.Id, u.Email, c.Name as Category, t.Amount, case t.Type when 0 then 'Expense' when 1 then 'Income' end as Type, t.Description, t.Date from Transactions t join Users u on u.Id = t.UserId join Categories c on c.Id = t.CategoryId order by t.Date desc;"
sqlite3 pennywise.db "select strftime('%Y-%m', Date) as Month, case Type when 0 then 'Expense' when 1 then 'Income' end as Type, sum(Amount) as Total from Transactions group by Month, Type order by Month desc, Type;"
```

## Build and Test

```powershell
dotnet build
dotnet test
```

If the running app locks files in `bin/Debug/...`, stop it or build to a temporary folder:

```powershell
dotnet build --no-restore -p:UseAppHost=false -o .\temp-build
```

## Troubleshooting

- **`sqlite3` not recognized** — install with `choco install sqlite -y`, then reopen PowerShell.
- **Chocolatey requires admin** — reopen PowerShell with **Run as Administrator**.
- **Database looks empty** — run `dotnet run` once; startup applies migrations and seeds demo data.
- **Build fails: `PennyWise.dll` locked** — the app is running; stop it or build to `temp-build` (see above).
- **SQLite database is locked** — close the running app or any DB browser holding `pennywise.db`.
