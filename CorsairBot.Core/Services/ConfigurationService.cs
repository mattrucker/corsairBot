using System.Text.Json;
using CorsairBot.Core.Models;

namespace CorsairBot.Core.Services;

public interface IConfigurationService
{
    CorsairBotConfiguration Configuration { get; }
    Task LoadConfigurationAsync();
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;
    public CorsairBotConfiguration Configuration { get; private set; }

    public ConfigurationService(ILogger<ConfigurationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configPath = configuration["ConfigPath"] ?? "/config/config.json";
        Configuration = new CorsairBotConfiguration();
    }

    public async Task LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException($"Configuration file not found at {_configPath}");
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<CorsairBotConfiguration>(json);

            if (config == null)
            {
                throw new JsonException("Failed to deserialize configuration file");
            }

            ValidateConfiguration(config);
            Configuration = config;
            _logger.LogInformation("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            throw;
        }
    }

    private void ValidateConfiguration(CorsairBotConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Region))
            throw new ArgumentException("Region is required");

        if (config.CorsairAccount == null || string.IsNullOrWhiteSpace(config.CorsairAccount.Username) || string.IsNullOrWhiteSpace(config.CorsairAccount.Password))
            throw new ArgumentException("Corsair account credentials are required");

        if (config.SkusToMonitor == null || !config.SkusToMonitor.Any())
            throw new ArgumentException("At least one SKU to monitor is required");

        if (config.GlobalPollingIntervalSeconds <= 0)
            throw new ArgumentException("Global polling interval must be greater than 0");

        if (config.MinimumActionDelaySeconds <= 0)
            throw new ArgumentException("Minimum action delay must be greater than 0");

        if (config.RetrySettings.MaxRetryAttempts <= 0)
            throw new ArgumentException("Max retry attempts must be greater than 0");

        if (config.RetrySettings.InitialBackoffSeconds <= 0)
            throw new ArgumentException("Initial backoff seconds must be greater than 0");

        if (config.RetrySettings.MaxBackoffSeconds <= 0)
            throw new ArgumentException("Max backoff seconds must be greater than 0");

        if (config.RetrySettings.BackoffMultiplier <= 0)
            throw new ArgumentException("Backoff multiplier must be greater than 0");
    }
} 