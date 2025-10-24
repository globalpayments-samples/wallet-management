using System.Text.Json.Serialization;

namespace CardPaymentSample;

/// <summary>
/// Standard API response format
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Health check data
/// </summary>
public class HealthData
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Service { get; set; } = "vault-one-click-dotnet";
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Payment method creation request
/// </summary>
public class PaymentMethodData
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string? StoredPaymentToken { get; set; } // Token from frontend tokenization

    [JsonPropertyName("payment_token")]
    public string? PaymentToken { get; set; } // Single-use payment token from GP PaymentForm

    public CardDetails? CardDetails { get; set; } // Card details from frontend
    public CustomerData? CustomerData { get; set; } // Customer information
    public string? Nickname { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    public BillingAddress? BillingAddress { get; set; }
    public string? Id { get; set; } // For editing existing payment methods

    // Flat customer properties to handle incoming JSON structure
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    [JsonPropertyName("street_address")]
    public string? StreetAddress { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    [JsonPropertyName("billing_zip")]
    public string? BillingZip { get; set; }

    public string? Country { get; set; }
}

/// <summary>
/// Customer data for multi-use token creation
/// </summary>
public class CustomerData
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? BillingZip { get; set; }
    public string? Country { get; set; }
}

/// <summary>
/// Card details from frontend tokenization
/// </summary>
public class CardDetails
{
    [JsonPropertyName("cardType")]
    public string? CardType { get; set; }

    [JsonPropertyName("cardLast4")]
    public string? CardLast4 { get; set; }

    [JsonPropertyName("expiryMonth")]
    public string? ExpiryMonth { get; set; }

    [JsonPropertyName("expiryYear")]
    public string? ExpiryYear { get; set; }

    [JsonPropertyName("cardNumber")]
    public string? CardNumber { get; set; }

    [JsonPropertyName("cardBin")]
    public string? CardBin { get; set; }

    [JsonPropertyName("cardSecurityCode")]
    public bool? CardSecurityCode { get; set; }

    [JsonPropertyName("cardholderName")]
    public string? CardholderName { get; set; }
}

/// <summary>
/// Multi-use token creation result
/// </summary>
public class MultiUseTokenResult
{
    public string MultiUseToken { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public CustomerData? CustomerData { get; set; }
}

/// <summary>
/// Billing address information
/// </summary>
public class BillingAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Country { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Stored payment method data
/// </summary>
public class StoredPaymentMethodData
{
    public string StoredPaymentToken { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool IsDefault { get; set; }
    public CustomerData? CustomerData { get; set; }
}

/// <summary>
/// Payment method as stored in JSON
/// </summary>
public class PaymentMethod
{
    public string Id { get; set; } = string.Empty;
    public string StoredPaymentToken { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool IsDefault { get; set; }
    public CustomerData? CustomerData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Payment method formatted for display
/// </summary>
public class FormattedPaymentMethod
{
    public string Id { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool IsDefault { get; set; }
    public bool MockMode { get; set; }
}

/// <summary>
/// Payment processing request
/// </summary>
public class PaymentRequest
{
    public string PaymentMethodId { get; set; } = string.Empty;
}

/// <summary>
/// Payment transaction response
/// </summary>
public class PaymentResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public GatewayResponse GatewayResponse { get; set; } = new();
}


/// <summary>
/// Gateway response details
/// </summary>
public class GatewayResponse
{
    public string AuthCode { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}

/// <summary>
/// Payment method info in responses
/// </summary>
public class PaymentMethodInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "card";
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
}


/// <summary>
/// Decline response
/// </summary>
public class DeclineResponse
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
}

/// <summary>
/// Charge response data
/// </summary>
public class ChargeResponseData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public GatewayResponse GatewayResponse { get; set; } = new();
    public PaymentMethodInfo PaymentMethod { get; set; } = new();
    public bool MockMode { get; set; }
}


/// <summary>
/// Mock mode configuration
/// </summary>
public class MockModeConfig
{
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Payment method edit request (nickname only)
/// </summary>
public class PaymentMethodEditData
{
    public string? Nickname { get; set; }
    public bool IsDefault { get; set; }
}