using Microsoft.EntityFrameworkCore;
using PennyWise.Data;

namespace PennyWise.Services;

public class RecurringBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _services;
    private readonly ILogger<RecurringBackgroundService> _logger;

    public RecurringBackgroundService(IServiceProvider services, ILogger<RecurringBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recurring transaction generation failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task GenerateDueAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var generated = await RecurringScheduler.GenerateAllAsync(db, DateTime.UtcNow.Date);
        if (generated > 0)
        {
            _logger.LogInformation("Generated {Count} recurring transactions.", generated);
        }
    }
}
