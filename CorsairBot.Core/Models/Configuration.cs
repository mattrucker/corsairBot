using System.Text.Json.Serialization;

namespace CorsairBot.Core.Models;

public class CorsairBotConfiguration
{
    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("corsair_account")]
    public CorsairAccount CorsairAccount { get; set; } = new();

    [JsonPropertyName("payment_details")]
    public PaymentDetails PaymentDetails { get; set; } = new();

    [JsonPropertyName("shipping_address")]
    public ShippingAddress ShippingAddress { get; set; } = new();

    [JsonPropertyName("skus_to_monitor")]
    public List<SkuToMonitor> SkusToMonitor { get; set; } = new();

    [JsonPropertyName("global_polling_interval_seconds")]
    public int GlobalPollingIntervalSeconds { get; set; }

    [JsonPropertyName("webdriver_settings")]
    public WebDriverSettings? WebDriverSettings { get; set; }

    [JsonPropertyName("logging_level")]
    public string LoggingLevel { get; set; } = "INFO";

    [JsonPropertyName("minimum_action_delay_seconds")]
    public int MinimumActionDelaySeconds { get; set; } = 10;

    [JsonPropertyName("retry_settings")]
    public RetrySettings RetrySettings { get; set; } = new();
}

public class CorsairAccount
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class PaymentDetails
{
    [JsonPropertyName("card_number")]
    public string CardNumber { get; set; } = string.Empty;

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; } = string.Empty;

    [JsonPropertyName("cvv")]
    public string Cvv { get; set; } = string.Empty;

    [JsonPropertyName("name_on_card")]
    public string NameOnCard { get; set; } = string.Empty;
}

public class ShippingAddress
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("address_line_1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [JsonPropertyName("address_line_2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state_province")]
    public string StateProvince { get; set; } = string.Empty;

    [JsonPropertyName("zip_postal_code")]
    public string ZipPostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SkuToMonitor
{
    [JsonPropertyName("sku_id")]
    public string SkuId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("target_quantity")]
    public int TargetQuantity { get; set; }

    [JsonPropertyName("polling_interval_seconds")]
    public int? PollingIntervalSeconds { get; set; }
}

public class WebDriverSettings
{
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
}

public class RetrySettings
{
    [JsonPropertyName("max_retry_attempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    [JsonPropertyName("initial_backoff_seconds")]
    public int InitialBackoffSeconds { get; set; } = 1;

    [JsonPropertyName("max_backoff_seconds")]
    public int MaxBackoffSeconds { get; set; } = 60;

    [JsonPropertyName("backoff_multiplier")]
    public float BackoffMultiplier { get; set; } = 2.0f;
} 