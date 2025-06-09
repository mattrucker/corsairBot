using System.Text.Json;
using CorsairBot.Core.Models;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Microsoft.Extensions.Logging; // Added for ILogger

namespace CorsairBot.Core.Services;

public interface IConfigurationService
{
    CorsairBotConfiguration Configuration { get; }
    Task LoadConfigurationAsync();
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration; // Added IConfiguration field
    private readonly string _configPath;
    public CorsairBotConfiguration Configuration { get; private set; }

    public ConfigurationService(ILogger<ConfigurationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration; // Store injected IConfiguration
        _configPath = _configuration["ConfigPath"] ?? "/config/config.json";
        Configuration = new CorsairBotConfiguration(); // Initialize to avoid null refs before loading
    }

    public async Task LoadConfigurationAsync()
    {
        CorsairBotConfiguration config;

        if (File.Exists(_configPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                config = JsonSerializer.Deserialize<CorsairBotConfiguration>(json) ?? new CorsairBotConfiguration();
                _logger.LogInformation("Configuration loaded from file: {ConfigPath}", _configPath);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to deserialize configuration file at {ConfigPath}. Initializing with default configuration.", _configPath);
                config = new CorsairBotConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An unexpected error occurred while reading configuration file at {ConfigPath}. Initializing with default configuration.", _configPath);
                config = new CorsairBotConfiguration();
            }
        }
        else
        {
            _logger.LogWarning("Configuration file not found at {ConfigPath}. Initializing with default configuration. Environment variables will be applied.", _configPath);
            config = new CorsairBotConfiguration();
        }

        _logger.LogInformation("Applying environment variable overrides...");

        // Ensure nested objects are initialized if config came from new CorsairBotConfiguration()
        config.CorsairAccount ??= new CorsairAccountConfiguration();
        config.PaymentDetails ??= new PaymentDetailsConfiguration();
        config.ShippingAddress ??= new ShippingAddressConfiguration();
        config.RetrySettings ??= new RetrySettingsConfiguration();
        config.WebDriverSettings ??= new WebDriverSettingsConfiguration();

        // Override with Environment Variables
        config.CorsairAccount.Username = _configuration["CORSAIR_ACCOUNT_USERNAME"] ?? config.CorsairAccount.Username;
        config.CorsairAccount.Password = _configuration["CORSAIR_ACCOUNT_PASSWORD"] ?? config.CorsairAccount.Password;

        config.PaymentDetails.CardNumber = _configuration["PAYMENT_CARD_NUMBER"] ?? config.PaymentDetails.CardNumber;
        config.PaymentDetails.ExpiryDate = _configuration["PAYMENT_EXPIRY_DATE"] ?? config.PaymentDetails.ExpiryDate;
        config.PaymentDetails.Cvv = _configuration["PAYMENT_CVV"] ?? config.PaymentDetails.Cvv;
        config.PaymentDetails.NameOnCard = _configuration["PAYMENT_NAME_ON_CARD"] ?? config.PaymentDetails.NameOnCard;

        config.ShippingAddress.FirstName = _configuration["SHIPPING_FIRST_NAME"] ?? config.ShippingAddress.FirstName;
        config.ShippingAddress.LastName = _configuration["SHIPPING_LAST_NAME"] ?? config.ShippingAddress.LastName;
        config.ShippingAddress.StreetAddress1 = _configuration["SHIPPING_STREET_ADDRESS_1"] ?? config.ShippingAddress.StreetAddress1;
        config.ShippingAddress.StreetAddress2 = _configuration["SHIPPING_STREET_ADDRESS_2"] ?? config.ShippingAddress.StreetAddress2;
        config.ShippingAddress.City = _configuration["SHIPPING_CITY"] ?? config.ShippingAddress.City;
        config.ShippingAddress.StateOrProvince = _configuration["SHIPPING_STATE_OR_PROVINCE"] ?? config.ShippingAddress.StateOrProvince;
        config.ShippingAddress.PostalCode = _configuration["SHIPPING_POSTAL_CODE"] ?? config.ShippingAddress.PostalCode;
        config.ShippingAddress.Country = _configuration["SHIPPING_COUNTRY"] ?? config.ShippingAddress.Country;
        config.ShippingAddress.PhoneNumber = _configuration["SHIPPING_PHONE_NUMBER"] ?? config.ShippingAddress.PhoneNumber;

        config.Region = _configuration["REGION"] ?? config.Region;

        if (int.TryParse(_configuration["GLOBAL_POLLING_INTERVAL_SECONDS"], out var globalPollingInterval))
        {
            config.GlobalPollingIntervalSeconds = globalPollingInterval;
        }
        if (Enum.TryParse<LogLevel>(_configuration["LOGGING_LEVEL"], true, out var loggingLevel))
        {
            config.LoggingLevel = loggingLevel;
        }
        if (int.TryParse(_configuration["MINIMUM_ACTION_DELAY_SECONDS"], out var minimumActionDelay))
        {
            config.MinimumActionDelaySeconds = minimumActionDelay;
        }

        // Retry Settings
        if (int.TryParse(_configuration["RETRY_MAX_ATTEMPTS"], out var maxAttempts))
        {
            config.RetrySettings.MaxRetryAttempts = maxAttempts;
        }
        if (int.TryParse(_configuration["RETRY_INITIAL_BACKOFF_SECONDS"], out var initialBackoff))
        {
            config.RetrySettings.InitialBackoffSeconds = initialBackoff;
        }
        if (int.TryParse(_configuration["RETRY_MAX_BACKOFF_SECONDS"], out var maxBackoff))
        {
            config.RetrySettings.MaxBackoffSeconds = maxBackoff;
        }
        if (double.TryParse(_configuration["RETRY_BACKOFF_MULTIPLIER"], out var backoffMultiplier))
        {
            config.RetrySettings.BackoffMultiplier = backoffMultiplier;
        }

        // WebDriver Settings
        config.WebDriverSettings.UserAgent = _configuration["WEBDRIVER_USER_AGENT"] ?? config.WebDriverSettings.UserAgent;

        // SkusToMonitor: For this iteration, primarily from JSON. If config was new(), it will be empty or null.
        // Consider logging if SkusToMonitor is empty or null after all loads.
        if (config.SkusToMonitor == null || !config.SkusToMonitor.Any())
        {
            _logger.LogWarning("SkusToMonitor is empty. Ensure it's configured either in the JSON file or via a future environment variable mechanism.");
        }

        Configuration = config;
        _logger.LogInformation("Environment variable overrides applied.");

        try
        {
            ValidateConfiguration(Configuration);
            _logger.LogInformation("Configuration validated successfully.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Configuration validation failed after applying environment variables.");
            throw; // Re-throw validation exceptions as they indicate critical misconfiguration
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during final configuration validation.");
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