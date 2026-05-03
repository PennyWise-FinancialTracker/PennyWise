using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Models;
using UglyToad.PdfPig;

namespace PennyWise.Pages.Dashboard;

public class TransactionsModel : DashboardPageModel
{
    private static readonly string[] CategoryColors = { "#4f46e5", "#14b8a6", "#9aa8f5", "#f4a300", "#ea4c89", "#22c55e", "#0ea5e9" };
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly Regex PdfDateRegex = new(
        @"\b(?<date>(?:\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?)|(?:\d{4}[/-]\d{1,2}[/-]\d{1,2})|(?:(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)[a-z]*\.?\s+\d{1,2}(?:,\s*\d{4})?))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PdfMoneyRegex = new(
        @"(?<![\d/])(?<amount>\(?-?\$?\d{1,3}(?:,\d{3})*(?:\.\d{2})?\)?|\(?-?\$?\d+\.\d{2}\)?)(?![\d/])",
        RegexOptions.Compiled);
    private static readonly Regex YearRegex = new(@"\b20\d{2}\b", RegexOptions.Compiled);
    private readonly AppDbContext _db;

    public TransactionsModel(AppDbContext db) => _db = db;

    public string Period { get; set; } = string.Empty;
    public List<TransactionRow> Transactions { get; set; } = new();
    public List<CategoryOption> CategoryOptions { get; set; } = new();
    public List<AccountOption> AccountOptions { get; set; } = new();
    public List<GoalOption> GoalOptions { get; set; } = new();
    public TransactionInput Input { get; set; } = new();
    public string MonthLabel { get; set; } = string.Empty;
    public Dictionary<string, string> PreviousMonthRoute { get; set; } = new();
    public Dictionary<string, string> NextMonthRoute { get; set; } = new();
    public Dictionary<string, string> PreviousPageRoute { get; set; } = new();
    public Dictionary<string, string> NextPageRoute { get; set; } = new();
    public ImportReviewSummary? ImportSummary { get; set; }
    public int TotalTransactions { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int ShowingFrom { get; set; }
    public int ShowingTo { get; set; }
    public int[] PageSizeOptions { get; } = { 10, 25, 50 };
    public string? PageMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Month { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public TransactionFilter Filter { get; set; } = new();

    [BindProperty]
    public List<ImportReviewInput> ImportRows { get; set; } = new();

    [BindProperty]
    public List<RecurringReviewInput> RecurringCandidates { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Input = new TransactionInput
        {
            Date = DateTime.Today,
            Type = TransactionType.Expense,
        };

        await LoadPageAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync([Bind(Prefix = "Input")] TransactionInput input)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        Input = input;
        if (!ModelState.IsValid)
        {
            await LoadPageAsync(userId);
            return Page();
        }

        if (!await CategoryIsVisibleAsync(userId, input.CategoryId))
        {
            ModelState.AddModelError("Input.CategoryId", "Choose a valid category.");
            await LoadPageAsync(userId);
            return Page();
        }

        if (input.AccountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == input.AccountId.Value && a.UserId == userId && !a.IsArchived))
        {
            ModelState.AddModelError("Input.AccountId", "Choose a valid account.");
            await LoadPageAsync(userId);
            return Page();
        }

        _db.Transactions.Add(new Transaction
        {
            UserId = userId,
            CategoryId = input.CategoryId,
            AccountId = input.AccountId,
            Amount = input.Amount,
            Description = input.Description.Trim(),
            Type = input.Type,
            Date = input.Date.Date,
        });
        await _db.SaveChangesAsync();

        Message = "Transaction added.";
        return RedirectToPage(new
        {
            Filter.Search,
            Filter.CategoryId,
            Filter.AccountId,
            Filter.Type,
            Filter.DateFrom,
            Filter.DateTo,
            Filter.Sort,
            Month,
            Year,
        });
    }

    public async Task<IActionResult> OnPostEditAsync(
        int id,
        DateTime date,
        string description,
        int categoryId,
        int? accountId,
        TransactionType type,
        decimal amount)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction is null)
        {
            Message = "Transaction not found.";
            return RedirectToPage(new { Month, Year });
        }

        if (amount <= 0 || string.IsNullOrWhiteSpace(description) || !await CategoryIsVisibleAsync(userId, categoryId) ||
            (accountId.HasValue && !await _db.Accounts.AnyAsync(a => a.Id == accountId.Value && a.UserId == userId)))
        {
            Message = "Transaction could not be updated. Check the amount, description, category, and account.";
            return RedirectToPage(new { Month, Year });
        }

        transaction.Date = date.Date;
        transaction.Description = description.Trim();
        transaction.CategoryId = categoryId;
        transaction.AccountId = accountId;
        transaction.Type = type;
        transaction.Amount = amount;
        await _db.SaveChangesAsync();

        Message = "Transaction updated.";
        return RedirectToPage(new { Month, Year });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var transaction = await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction is not null)
        {
            _db.Transactions.Remove(transaction);
            await _db.SaveChangesAsync();
            Message = "Transaction deleted.";
        }

        return RedirectToPage(new { Month, Year });
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var query = _db.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Filter.Search))
        {
            var search = Filter.Search.Trim();
            query = query.Where(t => t.Description.Contains(search) || (t.Category != null && t.Category.Name.Contains(search)));
        }

        if (Filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == Filter.CategoryId.Value);
        }

        if (Filter.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == Filter.AccountId.Value);
        }

        if (Filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == Filter.Type.Value);
        }

        if (Filter.DateFrom.HasValue)
        {
            query = query.Where(t => t.Date >= Filter.DateFrom.Value.Date);
        }

        if (Filter.DateTo.HasValue)
        {
            query = query.Where(t => t.Date <= Filter.DateTo.Value.Date);
        }

        if (!Filter.DateFrom.HasValue && !Filter.DateTo.HasValue && Month.HasValue && Year.HasValue)
        {
            var monthStart = new DateTime(Year.Value, Month.Value, 1);
            var monthEnd = monthStart.AddMonths(1);
            query = query.Where(t => t.Date >= monthStart && t.Date < monthEnd);
        }

        var rows = await query.OrderBy(t => t.Date).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Description,Category,Account,Type,Amount");
        foreach (var t in rows)
        {
            sb.Append(t.Date.ToString("yyyy-MM-dd", UsCulture));
            sb.Append(',');
            sb.Append(CsvEscape(t.Description));
            sb.Append(',');
            sb.Append(CsvEscape(t.Category?.Name ?? string.Empty));
            sb.Append(',');
            sb.Append(CsvEscape(t.Account?.Name ?? string.Empty));
            sb.Append(',');
            sb.Append(t.Type == TransactionType.Income ? "Income" : "Expense");
            sb.Append(',');
            sb.Append(t.Amount.ToString("0.00", UsCulture));
            sb.Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var filename = $"pennywise-transactions-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return File(bytes, "text/csv", filename);
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuoting) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public async Task<IActionResult> OnPostAllocateAsync(int goalId, decimal amount)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        if (amount <= 0)
        {
            Message = "Allocation amount must be greater than zero.";
            return RedirectToPage(new { Month, Year });
        }

        var goal = await _db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);
        if (goal is null)
        {
            Message = "Goal not found.";
            return RedirectToPage(new { Month, Year });
        }

        goal.CurrentAmount += amount;
        await _db.SaveChangesAsync();

        Message = $"Allocated {amount.ToString("C2", UsCulture)} to {goal.Name}.";
        return RedirectToPage(new { Month, Year });
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? statementFile)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        if (statementFile is null || statementFile.Length == 0)
        {
            Message = "Choose a bank statement PDF or transaction CSV to import.";
            return RedirectToPage(new { Month, Year });
        }

        var extension = Path.GetExtension(statementFile.FileName);
        var parsed = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
                     statementFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? await ParsePdfStatementAsync(statementFile)
            : await ParseCsvStatementAsync(statementFile);

        await LoadPageAsync(userId);
        await PrepareImportReviewAsync(userId, parsed);
        PageMessage = ImportRows.Count == 0
            ? "No transactions were detected. For PDFs, make sure it is a text-based bank statement, not a scanned image."
            : "Review the detected transactions before saving them.";
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmImportAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        var result = await ImportSelectedRowsAsync(userId, ImportRows, RecurringCandidates);
        Message = BuildImportMessage(result);
        return RedirectToPage(new { Month, Year });
    }

    private async Task<ImportResult> ImportSelectedRowsAsync(
        int userId,
        IReadOnlyList<ImportReviewInput> rows,
        IReadOnlyList<RecurringReviewInput> recurringCandidates)
    {
        if (rows.Count == 0)
        {
            return new ImportResult(0, 0, 0, 0);
        }

        var existingSignatures = (await _db.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new { t.Date, t.Description, t.Amount, t.Type })
                .ToListAsync())
            .Select(t => MakeDuplicateSignature(t.Date, t.Description, t.Amount, t.Type))
            .ToList();

        var imported = 0;
        var skipped = 0;
        var notSelected = 0;
        foreach (var importedRow in rows)
        {
            if (!importedRow.Selected)
            {
                notSelected++;
                continue;
            }

            if (importedRow.Amount <= 0 || string.IsNullOrWhiteSpace(importedRow.Description))
            {
                skipped++;
                continue;
            }

            var candidate = MakeDuplicateSignature(importedRow.Date, importedRow.Description, importedRow.Amount, importedRow.Type);
            if (IsFuzzyDuplicate(candidate, existingSignatures))
            {
                skipped++;
                continue;
            }
            existingSignatures.Add(candidate);

            var categoryName = string.IsNullOrWhiteSpace(importedRow.CategoryName)
                ? InferCategoryName(importedRow.Description, importedRow.Type)
                : importedRow.CategoryName;
            var category = await FindOrCreateCategoryAsync(userId, categoryName, importedRow.Type == TransactionType.Income);

            int? resolvedAccountId = importedRow.AccountId.HasValue && await _db.Accounts.AnyAsync(a => a.Id == importedRow.AccountId.Value && a.UserId == userId)
                ? importedRow.AccountId
                : null;

            _db.Transactions.Add(new Transaction
            {
                UserId = userId,
                CategoryId = category.Id,
                AccountId = resolvedAccountId,
                Amount = importedRow.Amount,
                Description = importedRow.Description,
                Type = importedRow.Type,
                Date = importedRow.Date.Date,
            });
            imported++;
        }

        await _db.SaveChangesAsync();
        var recurringCreated = await AddApprovedRecurringAsync(userId, recurringCandidates);
        return new ImportResult(imported, skipped, recurringCreated, notSelected);
    }

    private async Task PrepareImportReviewAsync(int userId, StatementParseResult parsed)
    {
        var existingSignatures = (await _db.Transactions
                .Where(t => t.UserId == userId)
                .Select(t => new { t.Date, t.Description, t.Amount, t.Type })
                .ToListAsync())
            .Select(t => MakeDuplicateSignature(t.Date, t.Description, t.Amount, t.Type))
            .ToList();

        var defaultAccountId = AccountOptions.Count > 0 ? AccountOptions[0].Id : (int?)null;

        ImportRows = parsed.Rows.Select((row, index) =>
        {
            var candidate = MakeDuplicateSignature(row.Date, row.Description, row.Amount, row.Type);
            var isDuplicate = IsFuzzyDuplicate(candidate, existingSignatures);
            return new ImportReviewInput
            {
                Selected = !isDuplicate,
                Date = row.Date,
                Description = row.Description,
                CategoryName = row.CategoryName,
                AccountId = defaultAccountId,
                Type = row.Type,
                Amount = row.Amount,
                IsDuplicate = isDuplicate,
                NeedsReview = row.NeedsReview,
                Note = row.Note,
                RowNumber = index + 1,
            };
        }).ToList();

        RecurringCandidates = await BuildRecurringCandidatesAsync(userId, parsed.Rows);

        ImportSummary = new ImportReviewSummary
        {
            Detected = parsed.Rows.Count,
            Skipped = parsed.SkippedRows,
            Duplicates = ImportRows.Count(r => r.IsDuplicate),
            NeedsReview = ImportRows.Count(r => r.NeedsReview),
            RecurringCandidates = RecurringCandidates.Count,
        };
    }

    private static async Task<StatementParseResult> ParseCsvStatementAsync(IFormFile csvFile)
    {
        var rows = new List<List<string>>();
        using var reader = new StreamReader(csvFile.OpenReadStream());
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(SplitCsvLine(line).ToList());
        }

        if (rows.Count == 0)
        {
            return new StatementParseResult(Array.Empty<ImportedTransaction>(), 0);
        }

        var startIndex = 0;
        var columnMap = CsvColumnMap.Positional();
        for (var i = 0; i < Math.Min(rows.Count, 10); i++)
        {
            var detected = CsvColumnMap.TryCreate(rows[i]);
            if (detected is null) continue;

            columnMap = detected;
            startIndex = i + 1;
            break;
        }

        var importedRows = new List<ImportedTransaction>();
        var skippedRows = 0;
        for (var i = startIndex; i < rows.Count; i++)
        {
            if (!TryBuildImportedTransaction(rows[i], columnMap, out var importedRow))
            {
                skippedRows++;
                continue;
            }

            importedRows.Add(importedRow);
        }

        return new StatementParseResult(importedRows, skippedRows);
    }

    private static async Task<StatementParseResult> ParsePdfStatementAsync(IFormFile pdfFile)
    {
        await using var stream = pdfFile.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            text.AppendLine(ExtractPdfPageText(page));
        }

        return ParsePdfText(text.ToString());
    }

    private static string ExtractPdfPageText(object page)
    {
        var wordsMethod = page.GetType().GetMethod("GetWords", Type.EmptyTypes);
        if (wordsMethod?.Invoke(page, null) is System.Collections.IEnumerable wordsEnumerable)
        {
            var words = new List<PdfWordText>();
            foreach (var word in wordsEnumerable)
            {
                var wordText = word?.GetType().GetProperty("Text")?.GetValue(word)?.ToString();
                var boundingBox = word?.GetType().GetProperty("BoundingBox")?.GetValue(word);
                if (string.IsNullOrWhiteSpace(wordText) || boundingBox is null) continue;

                var left = GetDoubleProperty(boundingBox, "Left") ?? GetDoubleProperty(boundingBox, "MinX") ?? 0;
                var top = GetDoubleProperty(boundingBox, "Top") ?? GetDoubleProperty(boundingBox, "MaxY") ?? 0;
                words.Add(new PdfWordText(wordText, left, top));
            }

            if (words.Count > 0)
            {
                return string.Join(
                    Environment.NewLine,
                    words.GroupBy(word => Math.Round(word.Top / 3) * 3)
                        .OrderByDescending(group => group.Key)
                        .Select(group => string.Join(" ", group.OrderBy(word => word.Left).Select(word => word.Text))));
            }
        }

        return page.GetType().GetProperty("Text")?.GetValue(page)?.ToString() ?? string.Empty;
    }

    private static double? GetDoubleProperty(object value, string propertyName)
    {
        var propertyValue = value.GetType().GetProperty(propertyName)?.GetValue(value);
        return propertyValue switch
        {
            double d => d,
            float f => f,
            decimal m => (double)m,
            _ => null,
        };
    }

    private async Task LoadPageAsync(int userId)
    {
        var selectedMonth = ResolveSelectedMonth();
        var nextMonth = selectedMonth.AddMonths(1);

        MonthLabel = selectedMonth.ToString("MMMM yyyy", UsCulture);
        Period = $"{MonthLabel} activity";
        PreviousMonthRoute = BuildMonthRoute(selectedMonth.AddMonths(-1));
        NextMonthRoute = BuildMonthRoute(nextMonth);

        CategoryOptions = await VisibleCategories(_db, userId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.Id, Name = c.Name, Color = c.Color, IsDefault = c.IsDefault })
            .ToListAsync();

        AccountOptions = await _db.Accounts
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderBy(a => a.Name)
            .Select(a => new AccountOption { Id = a.Id, Name = a.Name, Color = a.Color })
            .ToListAsync();

        GoalOptions = await _db.SavingsGoals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Name)
            .Select(g => new GoalOption { Id = g.Id, Name = g.Name })
            .ToListAsync();

        var query = _db.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .Include(t => t.Account)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Filter.Search))
        {
            var search = Filter.Search.Trim();
            query = query.Where(t => t.Description.Contains(search) || (t.Category != null && t.Category.Name.Contains(search)));
        }

        if (Filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == Filter.CategoryId.Value);
        }

        if (Filter.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == Filter.AccountId.Value);
        }

        if (Filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == Filter.Type.Value);
        }

        if (Filter.DateFrom.HasValue || Filter.DateTo.HasValue)
        {
            if (Filter.DateFrom.HasValue)
            {
                query = query.Where(t => t.Date >= Filter.DateFrom.Value.Date);
            }

            if (Filter.DateTo.HasValue)
            {
                query = query.Where(t => t.Date <= Filter.DateTo.Value.Date);
            }
        }
        else
        {
            query = query.Where(t => t.Date >= selectedMonth && t.Date < nextMonth);
        }

        NormalizePagination();
        TotalTransactions = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalTransactions / (double)Filter.PageSize));
        if (Filter.Page > TotalPages)
        {
            Filter.Page = TotalPages;
        }

        CurrentPage = Filter.Page;
        ShowingFrom = TotalTransactions == 0 ? 0 : ((CurrentPage - 1) * Filter.PageSize) + 1;
        ShowingTo = Math.Min(TotalTransactions, CurrentPage * Filter.PageSize);
        PreviousPageRoute = BuildPageRoute(Math.Max(1, CurrentPage - 1));
        NextPageRoute = BuildPageRoute(Math.Min(TotalPages, CurrentPage + 1));

        var rows = await ApplySort(query)
            .Skip((CurrentPage - 1) * Filter.PageSize)
            .Take(Filter.PageSize)
            .ToListAsync();

        Transactions = rows.Select(t => new TransactionRow
        {
            Id = t.Id,
            RawDate = t.Date,
            Date = t.Date.ToString("M/d/yyyy"),
            Description = t.Description,
            CategoryId = t.CategoryId,
            Category = t.Category?.Name ?? string.Empty,
            AccountId = t.AccountId,
            Account = t.Account?.Name ?? string.Empty,
            Type = t.Type,
            IsIncome = t.Type == TransactionType.Income,
            RawAmount = t.Amount,
            Amount = (t.Type == TransactionType.Income ? "+" : "-") + t.Amount.ToString("C2", UsCulture),
        }).ToList();
    }

    private IOrderedQueryable<Transaction> ApplySort(IQueryable<Transaction> query) =>
        (Filter.Sort ?? "date_desc") switch
        {
            "date_asc" => query.OrderBy(t => t.Date).ThenBy(t => t.Id),
            "amount_desc" => query.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Date),
            "amount_asc" => query.OrderBy(t => t.Amount).ThenByDescending(t => t.Date),
            "description_asc" => query.OrderBy(t => t.Description).ThenByDescending(t => t.Date),
            _ => query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id),
        };

    private DateTime ResolveSelectedMonth()
    {
        var today = DateTime.Today;
        var month = Month.GetValueOrDefault(today.Month);
        var year = Year.GetValueOrDefault(today.Year);

        if (month is < 1 or > 12)
        {
            month = today.Month;
        }

        if (year is < 2000 or > 2100)
        {
            year = today.Year;
        }

        Month = month;
        Year = year;
        return new DateTime(year, month, 1);
    }

    private Dictionary<string, string> BuildMonthRoute(DateTime month)
    {
        var route = new Dictionary<string, string>
        {
            ["Month"] = month.Month.ToString(CultureInfo.InvariantCulture),
            ["Year"] = month.Year.ToString(CultureInfo.InvariantCulture),
        };

        AddRouteValue(route, "Filter.Search", Filter.Search);
        AddRouteValue(route, "Filter.CategoryId", Filter.CategoryId?.ToString(CultureInfo.InvariantCulture));
        AddRouteValue(route, "Filter.AccountId", Filter.AccountId?.ToString(CultureInfo.InvariantCulture));
        AddRouteValue(route, "Filter.Type", Filter.Type?.ToString());
        AddRouteValue(route, "Filter.Sort", Filter.Sort);
        AddRouteValue(route, "Filter.PageSize", Filter.PageSize.ToString(CultureInfo.InvariantCulture));
        return route;
    }

    private Dictionary<string, string> BuildPageRoute(int page)
    {
        var route = new Dictionary<string, string>
        {
            ["Month"] = Month.GetValueOrDefault(DateTime.Today.Month).ToString(CultureInfo.InvariantCulture),
            ["Year"] = Year.GetValueOrDefault(DateTime.Today.Year).ToString(CultureInfo.InvariantCulture),
            ["Filter.Page"] = page.ToString(CultureInfo.InvariantCulture),
            ["Filter.PageSize"] = Filter.PageSize.ToString(CultureInfo.InvariantCulture),
            ["Filter.Sort"] = Filter.Sort,
        };

        AddRouteValue(route, "Filter.Search", Filter.Search);
        AddRouteValue(route, "Filter.CategoryId", Filter.CategoryId?.ToString(CultureInfo.InvariantCulture));
        AddRouteValue(route, "Filter.AccountId", Filter.AccountId?.ToString(CultureInfo.InvariantCulture));
        AddRouteValue(route, "Filter.Type", Filter.Type?.ToString());
        AddRouteValue(route, "Filter.DateFrom", Filter.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddRouteValue(route, "Filter.DateTo", Filter.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return route;
    }

    private void NormalizePagination()
    {
        if (!PageSizeOptions.Contains(Filter.PageSize))
        {
            Filter.PageSize = 10;
        }

        if (Filter.Page < 1)
        {
            Filter.Page = 1;
        }
    }

    private static void AddRouteValue(IDictionary<string, string> route, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            route[key] = value;
        }
    }

    private async Task<bool> CategoryIsVisibleAsync(int userId, int categoryId) =>
        await VisibleCategories(_db, userId).AnyAsync(c => c.Id == categoryId);

    private async Task<Category> FindOrCreateCategoryAsync(int userId, string name, bool isIncome)
    {
        var category = await VisibleCategories(_db, userId)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        if (category is not null) return category;

        var colorIndex = name
            .Aggregate(0, (hash, ch) => unchecked((hash * 31) + ch))
            % CategoryColors.Length;
        if (colorIndex < 0) colorIndex += CategoryColors.Length;

        category = new Category
        {
            UserId = userId,
            Name = name,
            Icon = name[..1].ToUpperInvariant(),
            Color = isIncome ? "#22c55e" : CategoryColors[colorIndex],
            IsDefault = false,
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private static IEnumerable<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"' && inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Add('"');
                i++;
            }
            else if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()).Trim());
                current.Clear();
            }
            else
            {
                current.Add(ch);
            }
        }

        values.Add(new string(current.ToArray()).Trim());
        return values;
    }

    private static StatementParseResult ParsePdfText(string text)
    {
        var imported = new List<ImportedTransaction>();
        var inferredYear = InferStatementYear(text);
        TransactionType? sectionType = null;
        var skippedRows = 0;

        foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = CleanDescription(rawLine);
            if (string.IsNullOrWhiteSpace(line)) continue;

            sectionType = DetectPdfSection(line, sectionType);
            if (TryBuildPdfTransaction(line, inferredYear, sectionType, out var row))
            {
                imported.Add(row);
            }
            else if (LooksLikeTransactionLine(line))
            {
                skippedRows++;
            }
        }

        return new StatementParseResult(imported, skippedRows);
    }

    private static bool TryBuildPdfTransaction(
        string line,
        int inferredYear,
        TransactionType? sectionType,
        out ImportedTransaction imported)
    {
        imported = default;
        if (IsPdfSummaryLine(line)) return false;

        var dateMatch = PdfDateRegex.Match(line);
        if (!dateMatch.Success) return false;

        var moneyMatches = PdfMoneyRegex.Matches(line)
            .Cast<Match>()
            .Where(m => m.Index > dateMatch.Index + dateMatch.Length)
            .Where(m => TryParseMoney(m.Groups["amount"].Value, out var parsed) && parsed != 0)
            .ToList();

        if (moneyMatches.Count == 0) return false;

        var amountMatch = moneyMatches[0];
        if (!TryParsePdfDate(dateMatch.Groups["date"].Value, inferredYear, out var date)) return false;
        if (!TryParseMoney(amountMatch.Groups["amount"].Value, out var signedAmount) || signedAmount == 0) return false;

        var descriptionStart = dateMatch.Index + dateMatch.Length;
        var descriptionLength = Math.Max(0, amountMatch.Index - descriptionStart);
        var description = descriptionLength > 0
            ? line.Substring(descriptionStart, descriptionLength)
            : line[(dateMatch.Index + dateMatch.Length)..];
        description = CleanPdfDescription(description);
        if (description.Length < 2 || IsPdfSummaryLine(description)) return false;

        var type = DeterminePdfTransactionType(line, amountMatch.Groups["amount"].Value, signedAmount, sectionType);
        var amount = Math.Abs(signedAmount);
        var categoryName = InferCategoryName(description, type);
        var needsReview = categoryName == "Uncategorized" || sectionType is null && type == TransactionType.Expense;
        var note = needsReview
            ? "Check the category and type before importing."
            : string.Empty;

        imported = new ImportedTransaction(date, description, categoryName, type, amount, needsReview, note);
        return true;
    }

    private static bool LooksLikeTransactionLine(string line) =>
        !IsPdfSummaryLine(line) && PdfDateRegex.IsMatch(line) && PdfMoneyRegex.IsMatch(line);

    private static TransactionType? DetectPdfSection(string line, TransactionType? current)
    {
        var text = line.ToLowerInvariant();
        if (ContainsAny(text, "deposits and additions", "deposits/additions", "credits and deposits", "deposits", "credits", "additions", "income"))
        {
            return TransactionType.Income;
        }

        if (ContainsAny(text, "withdrawals and debits", "withdrawals/debits", "checks paid", "checks", "debits", "purchases", "fees", "charges", "payments"))
        {
            return TransactionType.Expense;
        }

        return current;
    }

    private static TransactionType DeterminePdfTransactionType(
        string line,
        string rawAmount,
        decimal signedAmount,
        TransactionType? sectionType)
    {
        if (signedAmount < 0 || rawAmount.Contains('(') || rawAmount.TrimStart().StartsWith("-", StringComparison.Ordinal))
        {
            return TransactionType.Expense;
        }

        var text = line.ToLowerInvariant();
        if (ContainsAny(text, "deposit", "direct dep", "payroll", "salary", "refund", "interest paid", "interest earned", "credit", " cr"))
        {
            return TransactionType.Income;
        }

        if (ContainsAny(text, "debit", "withdrawal", "purchase", "fee", "charge", " dr"))
        {
            return TransactionType.Expense;
        }

        return sectionType ?? TransactionType.Expense;
    }

    private static string CleanPdfDescription(string value)
    {
        var withoutPostingDates = PdfDateRegex.Replace(value, " ");
        var withoutReferenceNoise = Regex.Replace(
            withoutPostingDates,
            @"\b(?:ref|reference|trace|auth|authorization|id|card|acct|account|seq|confirmation)\s*[:#]?\s*[a-z0-9-]{4,}\b",
            " ",
            RegexOptions.IgnoreCase);
        return CleanDescription(withoutReferenceNoise.Trim(' ', '-', '|', ':'));
    }

    private static bool IsPdfSummaryLine(string line)
    {
        var text = line.ToLowerInvariant();
        return ContainsAny(
            text,
            "beginning balance",
            "ending balance",
            "opening balance",
            "closing balance",
            "previous balance",
            "new balance",
            "available balance",
            "available credit",
            "minimum payment",
            "payment due",
            "statement balance",
            "statement date",
            "account number",
            "page ",
            "total deposits",
            "total withdrawals",
            "total debits",
            "total credits",
            "total fees",
            "subtotal");
    }

    private static int InferStatementYear(string text)
    {
        var years = YearRegex.Matches(text)
            .Select(m => int.TryParse(m.Value, out var year) ? year : 0)
            .Where(year => year is >= 2000 and <= 2100)
            .GroupBy(year => year)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();

        return years == 0 ? DateTime.Today.Year : years;
    }

    private static bool TryParsePdfDate(string value, int inferredYear, out DateTime date)
    {
        if (TryParseDate(value, out date)) return true;

        var normalized = value.Trim();
        if (Regex.IsMatch(normalized, @"^\d{1,2}[/-]\d{1,2}$"))
        {
            return TryParseDate($"{normalized}/{inferredYear}", out date);
        }

        if (Regex.IsMatch(normalized, @"^[a-z]{3,}\.?\s+\d{1,2}$", RegexOptions.IgnoreCase))
        {
            return TryParseDate($"{normalized} {inferredYear}", out date);
        }

        date = default;
        return false;
    }

    private static bool TryBuildImportedTransaction(
        IReadOnlyList<string> columns,
        CsvColumnMap map,
        out ImportedTransaction imported)
    {
        imported = default;

        if (!TryParseDate(GetColumn(columns, map.DateIndex), out var date)) return false;

        var description = CleanDescription(GetColumn(columns, map.DescriptionIndex));
        if (string.IsNullOrWhiteSpace(description)) return false;

        var explicitType = TryParseType(GetColumn(columns, map.TypeIndex), out var parsedType)
            ? parsedType
            : (TransactionType?)null;

        var type = explicitType ?? TransactionType.Expense;
        decimal amount;

        var creditValue = GetColumn(columns, map.CreditIndex);
        var debitValue = GetColumn(columns, map.DebitIndex);
        if (TryParseMoney(creditValue, out var credit) && credit != 0)
        {
            amount = Math.Abs(credit);
            type = TransactionType.Income;
        }
        else if (TryParseMoney(debitValue, out var debit) && debit != 0)
        {
            amount = Math.Abs(debit);
            type = TransactionType.Expense;
        }
        else if (TryParseMoney(GetColumn(columns, map.AmountIndex), out var signedAmount) && signedAmount != 0)
        {
            amount = Math.Abs(signedAmount);
            if (explicitType is null)
            {
                type = signedAmount < 0 ? TransactionType.Expense : TransactionType.Income;
            }
        }
        else
        {
            return false;
        }

        if (amount <= 0) return false;

        var categoryName = CleanDescription(GetColumn(columns, map.CategoryIndex));
        var categoryWasMissing = string.IsNullOrWhiteSpace(categoryName);
        if (categoryWasMissing)
        {
            categoryName = InferCategoryName(description, type);
        }

        var needsReview = categoryName == "Uncategorized" || categoryWasMissing && explicitType is null && map.CreditIndex < 0 && map.DebitIndex < 0;
        var note = needsReview
            ? "Check the category and type before importing."
            : string.Empty;

        imported = new ImportedTransaction(date, description, categoryName, type, amount, needsReview, note);
        return true;
    }

    private static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParse(value, UsCulture, DateTimeStyles.None, out date) ||
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool TryParseMoney(string value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var cleaned = value.Trim()
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("CR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DR", string.Empty, StringComparison.OrdinalIgnoreCase);

        return decimal.TryParse(cleaned, NumberStyles.Currency, UsCulture, out amount) ||
               decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static string GetColumn(IReadOnlyList<string> columns, int index) =>
        index >= 0 && index < columns.Count ? columns[index].Trim() : string.Empty;

    private static string CleanDescription(string value) =>
        string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string InferCategoryName(string description, TransactionType type)
    {
        if (type == TransactionType.Income) return "Income";

        var text = description.ToLowerInvariant();
        if (ContainsAny(text, "grocery", "groceries", "restaurant", "cafe", "coffee", "doordash", "ubereats", "grubhub", "mcdonald", "starbucks", "chipotle", "food")) return "Food & Dining";
        if (ContainsAny(text, "uber", "lyft", "gas", "fuel", "shell", "chevron", "exxon", "parking", "transit", "metro", "taxi")) return "Transportation";
        if (ContainsAny(text, "rent", "apartment", "landlord", "property management")) return "Rent";
        if (ContainsAny(text, "netflix", "spotify", "hulu", "disney", "subscription", "subscr", "apple.com/bill", "prime", "patreon")) return "Subscriptions";
        if (ContainsAny(text, "electric", "utility", "utilities", "water", "internet", "comcast", "xfinity", "verizon", "at&t", "phone", "t-mobile")) return "Utilities";
        if (ContainsAny(text, "pet", "pets", "vet", "chewy", "petco", "petsmart")) return "Pets";
        if (ContainsAny(text, "hotel", "airline", "flight", "airbnb", "travel", "delta", "southwest", "united airlines", "american airlines")) return "Travel";
        if (ContainsAny(text, "school", "tuition", "university", "college", "textbook", "student")) return "School";
        if (ContainsAny(text, "loan", "debt", "credit card payment", "student loan", "minimum payment")) return "Debt";
        if (ContainsAny(text, "pharmacy", "doctor", "medical", "dentist", "gym", "health", "cvs", "walgreens")) return "Health";
        if (ContainsAny(text, "amazon", "target", "walmart", "costco", "shopping", "store", "shop", "best buy")) return "Shopping";
        if (ContainsAny(text, "movie", "cinema", "concert", "ticketmaster", "game", "entertainment")) return "Entertainment";

        return "Uncategorized";
    }

    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static string ImportedMessage(int count) =>
        count == 1 ? "Imported 1 transaction." : $"Imported {count} transactions.";

    private async Task<List<RecurringReviewInput>> BuildRecurringCandidatesAsync(int userId, IReadOnlyList<ImportedTransaction> incomingRows)
    {
        var existingRecurringKeys = (await _db.RecurringTransactions
                .Where(r => r.UserId == userId)
                .Select(r => new { r.Description, r.Amount, r.Type })
                .ToListAsync())
            .Select(r => RecurringKey(r.Description, r.Amount, r.Type))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existingRows = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderBy(t => t.Date)
            .ToListAsync();
        var existingSources = existingRows
            .Select(t => new RecurringSource(
                t.Date,
                t.Description,
                t.Amount,
                t.Type,
                t.Category?.Name ?? string.Empty))
            .ToList();

        var incomingSources = incomingRows
            .Select(t => new RecurringSource(t.Date, t.Description, t.Amount, t.Type, t.CategoryName))
            .ToList();

        var candidates = new List<RecurringReviewInput>();
        foreach (var group in existingSources.Concat(incomingSources).GroupBy(t => RecurringKey(t.Description, t.Amount, t.Type)))
        {
            if (existingRecurringKeys.Contains(group.Key)) continue;

            var normalizedDescription = NormalizeRecurringDescription(group.First().Description);
            if (normalizedDescription.Length < 4 || IsTooGenericForRecurring(normalizedDescription)) continue;

            var monthlyOccurrences = group
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(monthGroup => monthGroup.OrderByDescending(t => t.Date).First())
                .OrderBy(t => t.Date)
                .ToList();

            if (monthlyOccurrences.Count < 2) continue;

            var daySpread = monthlyOccurrences.Max(t => t.Date.Day) - monthlyOccurrences.Min(t => t.Date.Day);
            if (daySpread > 7) continue;

            var earliest = monthlyOccurrences[0];
            var latest = monthlyOccurrences[^1];

            candidates.Add(new RecurringReviewInput
            {
                Selected = true,
                CategoryName = string.IsNullOrWhiteSpace(latest.CategoryName)
                    ? InferCategoryName(latest.Description, latest.Type)
                    : latest.CategoryName,
                Amount = latest.Amount,
                Description = latest.Description,
                Type = latest.Type,
                Frequency = RecurringFrequency.Monthly,
                StartDate = earliest.Date,
                LastGeneratedDate = latest.Date,
                Occurrences = monthlyOccurrences.Count,
            });
        }

        return candidates;
    }

    private async Task<int> AddApprovedRecurringAsync(int userId, IReadOnlyList<RecurringReviewInput> candidates)
    {
        var existingRecurringKeys = (await _db.RecurringTransactions
                .Where(r => r.UserId == userId)
                .Select(r => new { r.Description, r.Amount, r.Type })
                .ToListAsync())
            .Select(r => RecurringKey(r.Description, r.Amount, r.Type))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;
        foreach (var candidate in candidates.Where(c => c.Selected))
        {
            if (candidate.Amount <= 0 || string.IsNullOrWhiteSpace(candidate.Description)) continue;

            var key = RecurringKey(candidate.Description, candidate.Amount, candidate.Type);
            if (!existingRecurringKeys.Add(key)) continue;

            var categoryName = string.IsNullOrWhiteSpace(candidate.CategoryName)
                ? InferCategoryName(candidate.Description, candidate.Type)
                : candidate.CategoryName;
            var category = await FindOrCreateCategoryAsync(userId, categoryName, candidate.Type == TransactionType.Income);

            _db.RecurringTransactions.Add(new RecurringTransaction
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = candidate.Amount,
                Description = candidate.Description.Trim(),
                Type = candidate.Type,
                Frequency = candidate.Frequency,
                StartDate = candidate.StartDate.Date,
                LastGeneratedDate = candidate.LastGeneratedDate.Date,
                IsActive = true,
            });
            created++;
        }

        if (created > 0)
        {
            await _db.SaveChangesAsync();
        }

        return created;
    }

    private static bool IsTooGenericForRecurring(string value) =>
        value is "payment" or "purchase" or "withdrawal" or "deposit" or "transfer" or "online payment" or "debit card";

    private static string RecurringKey(string description, decimal amount, TransactionType type) =>
        $"{NormalizeRecurringDescription(description)}|{amount:0.00}|{(int)type}";

    private static string NormalizeRecurringDescription(string description)
    {
        var normalized = NormalizeHeader(description);
        var noiseWords = new[]
        {
            "pos", "debit", "card", "purchase", "payment", "online", "recurring", "autopay", "ach", "web",
            "transaction", "withdrawal", "deposit", "checkcard", "visa", "mastercard"
        };

        foreach (var noise in noiseWords)
        {
            normalized = normalized.Replace(noise, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return normalized.Length > 36 ? normalized[..36] : normalized;
    }

    private static string BuildImportMessage(ImportResult result)
    {
        if (result.Imported == 0)
        {
            return "No transactions were imported. Check the review rows and make sure at least one non-duplicate row is selected.";
        }

        var message = ImportedMessage(result.Imported);
        if (result.NotSelected > 0)
        {
            message += $" Left {result.NotSelected} row{(result.NotSelected == 1 ? string.Empty : "s")} unselected.";
        }

        if (result.Skipped > 0)
        {
            message += $" Skipped {result.Skipped} row{(result.Skipped == 1 ? string.Empty : "s")} that were duplicates or could not be read.";
        }

        if (result.DetectedRecurring > 0)
        {
            message += $" Added {result.DetectedRecurring} recurring item{(result.DetectedRecurring == 1 ? string.Empty : "s")}.";
        }

        return message;
    }

    private static string TransactionKey(DateTime date, string description, decimal amount, TransactionType type) =>
        $"{date:yyyy-MM-dd}|{CleanDescription(description).ToLowerInvariant()}|{amount:0.00}|{(int)type}";

    private record struct DuplicateSignature(DateTime Date, string Normalized, decimal Amount, TransactionType Type);

    private static DuplicateSignature MakeDuplicateSignature(DateTime date, string description, decimal amount, TransactionType type) =>
        new(date.Date, NormalizeForDuplicate(description), amount, type);

    private static string NormalizeForDuplicate(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;
        var lower = description.ToLowerInvariant();
        var keep = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            keep.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        var tokens = keep.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !DuplicateNoiseWords.Contains(t));
        return string.Join(' ', tokens);
    }

    private static readonly HashSet<string> DuplicateNoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "pos", "debit", "card", "purchase", "payment", "online", "ach", "web",
        "transaction", "withdrawal", "deposit", "checkcard", "visa", "mastercard",
        "recurring", "autopay", "ref", "id", "no",
    };

    private static bool IsFuzzyDuplicate(DuplicateSignature candidate, IReadOnlyCollection<DuplicateSignature> existing)
    {
        foreach (var sig in existing)
        {
            if (sig.Amount != candidate.Amount) continue;
            if (sig.Type != candidate.Type) continue;
            if (Math.Abs((sig.Date - candidate.Date).TotalDays) > 2) continue;

            if (string.IsNullOrEmpty(sig.Normalized) || string.IsNullOrEmpty(candidate.Normalized))
            {
                if (sig.Date == candidate.Date) return true;
                continue;
            }

            if (sig.Normalized == candidate.Normalized) return true;
            if (sig.Normalized.Contains(candidate.Normalized) ||
                candidate.Normalized.Contains(sig.Normalized)) return true;
        }
        return false;
    }

    private static bool TryParseType(string value, out TransactionType type)
    {
        var normalized = NormalizeHeader(value);
        if (normalized is "income" or "credit" or "deposit" or "directdeposit" or "payroll" or "refund" or "interest" or "incoming")
        {
            type = TransactionType.Income;
            return true;
        }

        if (normalized is "expense" or "debit" or "withdrawal" or "purchase" or "fee" or "charge" or "outgoing" or "cardpurchase")
        {
            type = TransactionType.Expense;
            return true;
        }

        return Enum.TryParse(value, true, out type);
    }

    private static string NormalizeHeader(string value) =>
        new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private readonly record struct ImportedTransaction(
        DateTime Date,
        string Description,
        string CategoryName,
        TransactionType Type,
        decimal Amount,
        bool NeedsReview = false,
        string Note = "");

    private readonly record struct StatementParseResult(
        IReadOnlyList<ImportedTransaction> Rows,
        int SkippedRows);

    private readonly record struct ImportResult(
        int Imported,
        int Skipped,
        int DetectedRecurring,
        int NotSelected);

    private readonly record struct PdfWordText(
        string Text,
        double Left,
        double Top);

    private readonly record struct RecurringSource(
        DateTime Date,
        string Description,
        decimal Amount,
        TransactionType Type,
        string CategoryName);

    private sealed class CsvColumnMap
    {
        public int DateIndex { get; init; } = -1;
        public int DescriptionIndex { get; init; } = -1;
        public int CategoryIndex { get; init; } = -1;
        public int TypeIndex { get; init; } = -1;
        public int AmountIndex { get; init; } = -1;
        public int DebitIndex { get; init; } = -1;
        public int CreditIndex { get; init; } = -1;

        public static CsvColumnMap Positional() => new()
        {
            DateIndex = 0,
            DescriptionIndex = 1,
            CategoryIndex = 2,
            TypeIndex = 3,
            AmountIndex = 4,
        };

        public static CsvColumnMap? TryCreate(IReadOnlyList<string> headers)
        {
            var normalized = headers.Select(NormalizeHeader).ToList();
            var map = new CsvColumnMap
            {
                DateIndex = FindDateHeader(normalized),
                DescriptionIndex = FindDescriptionHeader(normalized),
                CategoryIndex = FindHeader(normalized, h => h is "category" or "budgetcategory"),
                TypeIndex = FindHeader(normalized, h => h is "type" or "transactiontype" or "debitcredit"),
                AmountIndex = FindAmountHeader(normalized),
                DebitIndex = FindHeader(normalized, h => h.Contains("debit") || h.Contains("withdrawal") || h.Contains("outflow") || h.Contains("charge")),
                CreditIndex = FindHeader(normalized, h => h.Contains("credit") || h.Contains("deposit") || h.Contains("inflow")),
            };

            return map.DateIndex >= 0 &&
                   map.DescriptionIndex >= 0 &&
                   (map.AmountIndex >= 0 || map.DebitIndex >= 0 || map.CreditIndex >= 0)
                ? map
                : null;
        }

        private static int FindDateHeader(IReadOnlyList<string> headers) =>
            FindHeader(headers, h => h == "date" || h.EndsWith("date"));

        private static int FindDescriptionHeader(IReadOnlyList<string> headers) =>
            FindHeader(headers, h =>
                h.Contains("description") ||
                h is "desc" or "memo" or "merchant" or "payee" or "name" or "details" or "narrative");

        private static int FindAmountHeader(IReadOnlyList<string> headers) =>
            FindHeader(headers, h =>
                !h.Contains("balance") &&
                (h == "amount" || h.StartsWith("amount") || h.EndsWith("amount") || h.Contains("transactionamount")));

        private static int FindHeader(IReadOnlyList<string> headers, Func<string, bool> predicate)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                if (predicate(headers[i])) return i;
            }

            return -1;
        }
    }

    public class TransactionFilter
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? AccountId { get; set; }
        public TransactionType? Type { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        public string Sort { get; set; } = "date_desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ImportReviewInput
    {
        public int RowNumber { get; set; }
        public bool Selected { get; set; } = true;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(256)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string CategoryName { get; set; } = string.Empty;

        public int? AccountId { get; set; }

        public TransactionType Type { get; set; }

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }

        public bool IsDuplicate { get; set; }
        public bool NeedsReview { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class RecurringReviewInput
    {
        public bool Selected { get; set; } = true;

        [Required]
        [StringLength(256)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string CategoryName { get; set; } = string.Empty;

        public TransactionType Type { get; set; }

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }

        public RecurringFrequency Frequency { get; set; } = RecurringFrequency.Monthly;

        public DateTime StartDate { get; set; }

        public DateTime LastGeneratedDate { get; set; }

        public int Occurrences { get; set; }
    }

    public class ImportReviewSummary
    {
        public int Detected { get; set; }
        public int Skipped { get; set; }
        public int Duplicates { get; set; }
        public int NeedsReview { get; set; }
        public int RecurringCandidates { get; set; }
    }

    public class TransactionInput
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [StringLength(256)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
        public int CategoryId { get; set; }

        [Display(Name = "Account")]
        public int? AccountId { get; set; }

        [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; } = TransactionType.Expense;
    }
}
