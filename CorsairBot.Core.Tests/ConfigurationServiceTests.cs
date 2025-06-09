using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CorsairBot.Core.Services;
using CorsairBot.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;

namespace CorsairBot.Core.Tests
{
    public class ConfigurationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ConfigurationService>> _mockLogger;
        private string? _tempConfigFilePath;

        public ConfigurationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ConfigurationService>>();
        }

        private IConfigurationRoot BuildConfiguration(Dictionary<string, string?> configValues)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        }

        private void CreateTempConfigFile(string content)
        {
            _tempConfigFilePath = Path.GetTempFileName();
            File.WriteAllText(_tempConfigFilePath, content);
        }

        public void Dispose()
        {
            if (_tempConfigFilePath != null && File.Exists(_tempConfigFilePath))
            {
                File.Delete(_tempConfigFilePath);
                _tempConfigFilePath = null;
            }
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task LoadConfigurationAsync_EnvironmentVariablesOnly_LoadsSuccessfully()
        {
            // Arrange
            var envVars = new Dictionary<string, string?>
            {
                { "CORSAIR_ACCOUNT_USERNAME", "testUser" },
                { "CORSAIR_ACCOUNT_PASSWORD", "testPass" },
                { "PAYMENT_CARD_NUMBER", "1234567890123456" },
                { "PAYMENT_EXPIRY_DATE", "12/25" },
                { "PAYMENT_CVV", "123" },
                { "PAYMENT_NAME_ON_CARD", "Test User" },
                { "SHIPPING_FIRST_NAME", "Test" },
                { "SHIPPING_LAST_NAME", "User" },
                { "SHIPPING_STREET_ADDRESS_1", "123 Main St" },
                { "SHIPPING_CITY", "Testville" },
                { "SHIPPING_STATE_OR_PROVINCE", "TS" },
                { "SHIPPING_POSTAL_CODE", "12345" },
                { "SHIPPING_COUNTRY", "US" },
                { "SHIPPING_PHONE_NUMBER", "555-1234" },
                { "REGION", "us-test-env" },
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "60" },
                { "LOGGING_LEVEL", "Information" },
                { "MINIMUM_ACTION_DELAY_SECONDS", "5" },
                { "RETRY_MAX_ATTEMPTS", "3" },
                { "RETRY_INITIAL_BACKOFF_SECONDS", "1" },
                { "RETRY_MAX_BACKOFF_SECONDS", "10" },
                { "RETRY_BACKOFF_MULTIPLIER", "1.5" },
                { "WEBDRIVER_USER_AGENT", "TestAgent/1.0" },
                { "ConfigPath", "non_existent_config.json" } // Ensure file loading is skipped
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act
            await configService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(configService.Configuration);
            Assert.Equal("testUser", configService.Configuration.CorsairAccount?.Username);
            Assert.Equal("testPass", configService.Configuration.CorsairAccount?.Password);
            Assert.Equal("1234567890123456", configService.Configuration.PaymentDetails?.CardNumber);
            Assert.Equal("us-test-env", configService.Configuration.Region);
            Assert.Equal(60, configService.Configuration.GlobalPollingIntervalSeconds);
            Assert.True(configService.Configuration.SkusToMonitor == null || !configService.Configuration.SkusToMonitor.Any());
        }

        [Fact]
        public async Task LoadConfigurationAsync_FileAndEnvVars_LoadsSuccessfully()
        {
            // Arrange
            var fileData = new CorsairBotConfiguration
            {
                Region = "us-file",
                GlobalPollingIntervalSeconds = 120,
                SkusToMonitor = new List<SkuToMonitor> { new SkuToMonitor { Sku = "SKU123", PollIntervalSeconds = 30 } },
                // Minimal other required fields for file to be valid if it were the only source (not strictly needed for this test's focus)
                CorsairAccount = new CorsairAccountConfiguration { Username = "fileUser", Password = "filePassword"},
                PaymentDetails = new PaymentDetailsConfiguration { CardNumber = "fileCard", ExpiryDate="fileExp", Cvv="fileCvv", NameOnCard="file Name"},
                MinimumActionDelaySeconds = 1,
                RetrySettings = new RetrySettingsConfiguration { MaxRetryAttempts = 1, InitialBackoffSeconds = 1, MaxBackoffSeconds = 1, BackoffMultiplier = 1}
            };
            CreateTempConfigFile(JsonSerializer.Serialize(fileData));

            var envVars = new Dictionary<string, string?>
            {
                { "ConfigPath", _tempConfigFilePath },
                { "CORSAIR_ACCOUNT_USERNAME", "envUser" }, // This should override fileUser if present in file
                { "CORSAIR_ACCOUNT_PASSWORD", "envPass" }, // This should override filePassword
                { "PAYMENT_CARD_NUMBER", "envCard123" },   // This should override fileCard
                // REGION and GLOBAL_POLLING_INTERVAL_SECONDS are NOT set in env, should come from file
                 { "PAYMENT_EXPIRY_DATE", "envExp" },
                 { "PAYMENT_CVV", "envCvv" },
                 { "PAYMENT_NAME_ON_CARD", "env Name" },
                 { "SHIPPING_FIRST_NAME", "envShipFirst" }, // Example of other required fields from env
                 { "SHIPPING_LAST_NAME", "envShipLast" },
                 { "SHIPPING_STREET_ADDRESS_1", "envAddr1" },
                 { "SHIPPING_CITY", "envCity" },
                 { "SHIPPING_STATE_OR_PROVINCE", "envState" },
                 { "SHIPPING_POSTAL_CODE", "envZip" },
                 { "SHIPPING_COUNTRY", "envCountry" },
                 { "SHIPPING_PHONE_NUMBER", "envPhone" },
                 { "MINIMUM_ACTION_DELAY_SECONDS", "5" }, // from env
                 { "RETRY_MAX_ATTEMPTS", "2" }, // from env
                 { "RETRY_INITIAL_BACKOFF_SECONDS", "2" }, // from env
                 { "RETRY_MAX_BACKOFF_SECONDS", "20" }, // from env
                 { "RETRY_BACKOFF_MULTIPLIER", "2.0" } // from env
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act
            await configService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(configService.Configuration);
            // Values from file
            Assert.Equal("us-file", configService.Configuration.Region);
            Assert.Equal(120, configService.Configuration.GlobalPollingIntervalSeconds);
            Assert.NotNull(configService.Configuration.SkusToMonitor);
            Assert.Single(configService.Configuration.SkusToMonitor);
            Assert.Equal("SKU123", configService.Configuration.SkusToMonitor[0].Sku);
            // Values from environment variables (secrets and overrides)
            Assert.Equal("envUser", configService.Configuration.CorsairAccount?.Username);
            Assert.Equal("envPass", configService.Configuration.CorsairAccount?.Password);
            Assert.Equal("envCard123", configService.Configuration.PaymentDetails?.CardNumber);
        }

        [Fact]
        public async Task LoadConfigurationAsync_EnvVarsOverrideFile_OverridesCorrectly()
        {
            // Arrange
            var fileData = new CorsairBotConfiguration
            {
                Region = "file_region",
                GlobalPollingIntervalSeconds = 100,
                CorsairAccount = new CorsairAccountConfiguration { Username = "fileUser", Password="filePassword" },
                PaymentDetails = new PaymentDetailsConfiguration { CardNumber = "fileCard", ExpiryDate="fileExp", Cvv="fileCvv", NameOnCard="file Name"},
                SkusToMonitor = new List<SkuToMonitor> { new SkuToMonitor { Sku = "SKU_FILE" } },
                MinimumActionDelaySeconds = 1,
                RetrySettings = new RetrySettingsConfiguration { MaxRetryAttempts = 1, InitialBackoffSeconds = 1, MaxBackoffSeconds = 1, BackoffMultiplier = 1}
            };
            CreateTempConfigFile(JsonSerializer.Serialize(fileData));

            var envVars = new Dictionary<string, string?>
            {
                { "ConfigPath", _tempConfigFilePath },
                { "REGION", "env_region" }, // This should override "file_region"
                { "CORSAIR_ACCOUNT_USERNAME", "envUser" },
                { "CORSAIR_ACCOUNT_PASSWORD", "envPass" },
                { "PAYMENT_CARD_NUMBER", "1234567890123456" },
                { "PAYMENT_EXPIRY_DATE", "12/25" },
                { "PAYMENT_CVV", "123" },
                { "PAYMENT_NAME_ON_CARD", "Test User" },
                { "SHIPPING_FIRST_NAME", "Test" },
                { "SHIPPING_LAST_NAME", "User" },
                { "SHIPPING_STREET_ADDRESS_1", "123 Main St" },
                { "SHIPPING_CITY", "Testville" },
                { "SHIPPING_STATE_OR_PROVINCE", "TS" },
                { "SHIPPING_POSTAL_CODE", "12345" },
                { "SHIPPING_COUNTRY", "US" },
                { "SHIPPING_PHONE_NUMBER", "555-1234" },
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "60" }, // Should override 100 from file
                { "LOGGING_LEVEL", "Information" },
                { "MINIMUM_ACTION_DELAY_SECONDS", "5" },
                { "RETRY_MAX_ATTEMPTS", "3" },
                { "RETRY_INITIAL_BACKOFF_SECONDS", "1" },
                { "RETRY_MAX_BACKOFF_SECONDS", "10" },
                { "RETRY_BACKOFF_MULTIPLIER", "1.5" }
                // SkusToMonitor are not provided by env, so file version should persist
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act
            await configService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(configService.Configuration);
            Assert.Equal("env_region", configService.Configuration.Region); // Overridden by env
            Assert.Equal(60, configService.Configuration.GlobalPollingIntervalSeconds); // Overridden by env
            Assert.NotNull(configService.Configuration.SkusToMonitor);
            Assert.Single(configService.Configuration.SkusToMonitor);
            Assert.Equal("SKU_FILE", configService.Configuration.SkusToMonitor[0].Sku); // From file
        }

        [Fact]
        public async Task LoadConfigurationAsync_MissingRequiredConfig_ThrowsArgumentException()
        {
            // Arrange
            var envVars = new Dictionary<string, string?>
            {
                // Missing CORSAIR_ACCOUNT_USERNAME
                { "CORSAIR_ACCOUNT_PASSWORD", "testPass" },
                { "PAYMENT_CARD_NUMBER", "1234567890123456" },
                { "PAYMENT_EXPIRY_DATE", "12/25" },
                { "PAYMENT_CVV", "123" },
                { "PAYMENT_NAME_ON_CARD", "Test User" },
                { "REGION", "us-test" }, // This is present
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "60" },
                { "MINIMUM_ACTION_DELAY_SECONDS", "5" },
                { "RETRY_MAX_ATTEMPTS", "3" },
                { "RETRY_INITIAL_BACKOFF_SECONDS", "1" },
                { "RETRY_MAX_BACKOFF_SECONDS", "10" },
                { "RETRY_BACKOFF_MULTIPLIER", "1.5" },
                { "ConfigPath", "non_existent_config.json" }
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => configService.LoadConfigurationAsync());
            // Optional: Check message if it's specific, e.g. contains "Corsair account credentials are required"
            Assert.Contains("Corsair account credentials are required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoadConfigurationAsync_MissingRequiredRegion_ThrowsArgumentException()
        {
            // Arrange
            var envVars = new Dictionary<string, string?>
            {
                { "CORSAIR_ACCOUNT_USERNAME", "testUser" },
                { "CORSAIR_ACCOUNT_PASSWORD", "testPass" },
                { "PAYMENT_CARD_NUMBER", "1234567890123456" },
                // ... other necessary payment/shipping details to pass those validations
                { "PAYMENT_EXPIRY_DATE", "12/25" },
                { "PAYMENT_CVV", "123" },
                { "PAYMENT_NAME_ON_CARD", "Test User" },
                { "SHIPPING_FIRST_NAME", "Test" },
                { "SHIPPING_LAST_NAME", "User" },
                { "SHIPPING_STREET_ADDRESS_1", "123 Main St" },
                { "SHIPPING_CITY", "Testville" },
                { "SHIPPING_STATE_OR_PROVINCE", "TS" },
                { "SHIPPING_POSTAL_CODE", "12345" },
                { "SHIPPING_COUNTRY", "US" },
                { "SHIPPING_PHONE_NUMBER", "555-1234" },
                // REGION is missing
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "60" },
                { "MINIMUM_ACTION_DELAY_SECONDS", "5" },
                { "RETRY_MAX_ATTEMPTS", "3" },
                { "RETRY_INITIAL_BACKOFF_SECONDS", "1" },
                { "RETRY_MAX_BACKOFF_SECONDS", "10" },
                { "RETRY_BACKOFF_MULTIPLIER", "1.5" },
                { "ConfigPath", "non_existent_config.json" }
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => configService.LoadConfigurationAsync());
            Assert.Contains("Region is required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }


        [Fact]
        public async Task LoadConfigurationAsync_ParsesNumericTypesCorrectly()
        {
            // Arrange
            var envVars = new Dictionary<string, string?>
            {
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "123" },
                { "RETRY_MAX_ATTEMPTS", "5" },
                { "RETRY_BACKOFF_MULTIPLIER", "2.5" }, // Use . for decimal for CultureInfo consistency in parsing
                { "MINIMUM_ACTION_DELAY_SECONDS", "15"},
                { "RETRY_INITIAL_BACKOFF_SECONDS", "2"},
                { "RETRY_MAX_BACKOFF_SECONDS", "30"},
                // Provide all other required string fields
                { "CORSAIR_ACCOUNT_USERNAME", "numUser" },
                { "CORSAIR_ACCOUNT_PASSWORD", "numPass" },
                { "PAYMENT_CARD_NUMBER", "1234509876543210" },
                { "PAYMENT_EXPIRY_DATE", "01/26" },
                { "PAYMENT_CVV", "321" },
                { "PAYMENT_NAME_ON_CARD", "Numeric Test" },
                { "SHIPPING_FIRST_NAME", "Num" },
                { "SHIPPING_LAST_NAME", "Test" },
                { "SHIPPING_STREET_ADDRESS_1", "456 Test Ave" },
                { "SHIPPING_CITY", "NumericCity" },
                { "SHIPPING_STATE_OR_PROVINCE", "NT" },
                { "SHIPPING_POSTAL_CODE", "54321" },
                { "SHIPPING_COUNTRY", "NC" },
                { "SHIPPING_PHONE_NUMBER", "555-6789" },
                { "REGION", "num-region" },
                { "ConfigPath", "non_existent_config.json" }
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act
            await configService.LoadConfigurationAsync();

            // Assert
            Assert.NotNull(configService.Configuration);
            Assert.Equal(123, configService.Configuration.GlobalPollingIntervalSeconds);
            Assert.Equal(5, configService.Configuration.RetrySettings?.MaxRetryAttempts);
            Assert.Equal(2.5, configService.Configuration.RetrySettings?.BackoffMultiplier); // double comparison
        }

        [Fact]
        public async Task LoadConfigurationAsync_InvalidNumericFormat_ThrowsArgumentExceptionOnValidation()
        {
            // Arrange
            var envVars = new Dictionary<string, string?>
            {
                { "GLOBAL_POLLING_INTERVAL_SECONDS", "abc" }, // Invalid format
                // Provide all other required fields with valid values
                { "CORSAIR_ACCOUNT_USERNAME", "invalidNumUser" },
                { "CORSAIR_ACCOUNT_PASSWORD", "invalidNumPass" },
                { "PAYMENT_CARD_NUMBER", "0987654321098765" },
                { "PAYMENT_EXPIRY_DATE", "02/27" },
                { "PAYMENT_CVV", "456" },
                { "PAYMENT_NAME_ON_CARD", "Invalid Numeric Test" },
                { "SHIPPING_FIRST_NAME", "Invalid" },
                { "SHIPPING_LAST_NAME", "NumTest" },
                { "SHIPPING_STREET_ADDRESS_1", "789 Invalid St" },
                { "SHIPPING_CITY", "InvalidCity" },
                { "SHIPPING_STATE_OR_PROVINCE", "IN" },
                { "SHIPPING_POSTAL_CODE", "67890" },
                { "SHIPPING_COUNTRY", "IC" },
                { "SHIPPING_PHONE_NUMBER", "555-1122" },
                { "REGION", "invalid-num-region" },
                { "MINIMUM_ACTION_DELAY_SECONDS", "10" }, // Valid
                { "RETRY_MAX_ATTEMPTS", "3" }, // Valid
                { "RETRY_INITIAL_BACKOFF_SECONDS", "5" }, // Valid
                { "RETRY_MAX_BACKOFF_SECONDS", "50" }, // Valid
                { "RETRY_BACKOFF_MULTIPLIER", "2.0" }, // Valid
                { "ConfigPath", "non_existent_config.json" }
            };
            var configuration = BuildConfiguration(envVars);
            var configService = new ConfigurationService(_mockLogger.Object, configuration);

            // Act & Assert
            // LoadConfigurationAsync itself won't throw for "abc" due to TryParse.
            // The default value (0 for int) will then fail ValidateConfiguration.
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => configService.LoadConfigurationAsync());
            Assert.Contains("Global polling interval must be greater than 0", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
