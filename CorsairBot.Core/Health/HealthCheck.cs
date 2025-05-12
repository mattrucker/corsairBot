using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CorsairBot.Core.Health;

public class CorsairBotHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<CorsairBotHealthCheck> _logger;
    private DateTime _lastSuccessfulAction = DateTime.UtcNow;
    private int _errorCount = 0;

    public CorsairBotHealthCheck(
        IConfigurationService configurationService,
        ILogger<CorsairBotHealthCheck> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _configurationService.Configuration;
            var data = new Dictionary<string, object>
            {
                { "uptime", DateTime.UtcNow - _lastSuccessfulAction },
                { "last_successful_action", _lastSuccessfulAction },
                { "error_count", _errorCount },
                { "monitoring_skus", config.SkusToMonitor.Count },
                { "region", config.Region }
            };

            return Task.FromResult(HealthCheckResult.Healthy("CorsairBot is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("CorsairBot is unhealthy", ex));
        }
    }

    public void UpdateLastSuccessfulAction()
    {
        _lastSuccessfulAction = DateTime.UtcNow;
    }

    public void IncrementErrorCount()
    {
        _errorCount++;
    }
} 