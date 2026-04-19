using Microsoft.EntityFrameworkCore;
using PennyWise.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Auth/Login", "");
    options.Conventions.AddPageRoute("/Auth/Login", "login");
    options.Conventions.AddPageRoute("/Auth/Signup", "signup");
    options.Conventions.AddPageRoute("/Dashboard/Overview", "overview");
    options.Conventions.AddPageRoute("/Dashboard/Transactions", "transactions");
    options.Conventions.AddPageRoute("/Dashboard/Budgets", "budgets");
    options.Conventions.AddPageRoute("/Dashboard/Analytics", "analytics");
});

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

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
