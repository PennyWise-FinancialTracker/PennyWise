using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PennyWise.Data;
using PennyWise.Data.Entities;
using PennyWise.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Dashboard");
    options.Conventions.AddPageRoute("/Auth/Login", "");
    options.Conventions.AddPageRoute("/Auth/Login", "login");
    options.Conventions.AddPageRoute("/Auth/Signup", "signup");
    options.Conventions.AddPageRoute("/Dashboard/Overview", "overview");
    options.Conventions.AddPageRoute("/Dashboard/Transactions", "transactions");
    options.Conventions.AddPageRoute("/Dashboard/Accounts", "accounts");
    options.Conventions.AddPageRoute("/Dashboard/Budgets", "budgets");
    options.Conventions.AddPageRoute("/Dashboard/Categories", "categories");
    options.Conventions.AddPageRoute("/Dashboard/Recurring", "recurring");
    options.Conventions.AddPageRoute("/Dashboard/Goals", "goals");
    options.Conventions.AddPageRoute("/Dashboard/Analytics", "analytics");
    options.Conventions.AddPageRoute("/Dashboard/Settings", "settings");
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (provider.ToLowerInvariant())
    {
        case "postgres":
        case "postgresql":
        case "npgsql":
            options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
            break;
        case "sqlite":
        default:
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
            break;
    }
});

builder.Services.AddHostedService<RecurringBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();
