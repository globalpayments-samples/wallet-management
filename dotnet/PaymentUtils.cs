using GlobalPayments.Api.Entities;
using GlobalPayments.Api.PaymentMethods;
using System.Text.RegularExpressions;

namespace CardPaymentSample;

/// <summary>
/// Payment utility functions for Global Payments SDK
/// </summary>
public static class PaymentUtils
{
    /// <summary>
    /// Sanitize postal code by removing invalid characters
    /// </summary>
    public static string SanitizePostalCode(string? postalCode)
    {
        if (string.IsNullOrEmpty(postalCode))
            return string.Empty;

        var sanitized = Regex.Replace(postalCode, "[^a-zA-Z0-9-]", "");
        return sanitized.Length > 10 ? sanitized[..10] : sanitized;
    }

    /// <summary>
    /// Determine card brand from Global Payments card type
    /// </summary>
    public static string DetermineCardBrandFromType(string cardType)
    {
        if (string.IsNullOrEmpty(cardType)) return "Unknown";

        var type = cardType.ToLower();

        return type switch
        {
            "visa" => "Visa",
            "mastercard" or "mc" => "Mastercard",
            "amex" or "americanexpress" => "American Express",
            "discover" => "Discover",
            "jcb" => "JCB",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Create multi-use token with customer data attached (GP API)
    /// Uses charge-based approach to convert single-use to multi-use token
    /// </summary>
    public static async Task<MultiUseTokenResult> CreateMultiUseTokenWithCustomerAsync(string paymentToken, CustomerData customerData, CardDetails cardDetails)
    {
        return await Task.Run(() =>
        {
            try
            {
                var card = new CreditCardData
                {
                    Token = paymentToken,
                    CardHolderName = $"{customerData.FirstName?.Trim() ?? ""} {customerData.LastName?.Trim() ?? ""}".Trim()
                };

                // Create address from customer data
                var address = new Address
                {
                    StreetAddress1 = customerData.StreetAddress?.Trim() ?? "",
                    City = customerData.City?.Trim() ?? "",
                    Province = customerData.State?.Trim() ?? "",
                    PostalCode = SanitizePostalCode(customerData.BillingZip),
                    Country = customerData.Country?.Trim() ?? ""
                };

                // Charge to convert single-use to multi-use token
                // GP API requires a charge (not verify) to create multi-use token
                var response = card.Charge(0.01m)
                    .WithCurrency("GBP")
                    .WithRequestMultiUseToken(true)
                    .WithAddress(address)
                    .Execute();

                if (response.ResponseCode == "SUCCESS" &&
                    response.ResponseMessage == "CAPTURED")
                {
                    var brand = DetermineCardBrandFromType(cardDetails.CardType ?? "");
                    var multiUseToken = response.Token ?? paymentToken;

                    return new MultiUseTokenResult
                    {
                        MultiUseToken = multiUseToken,
                        Brand = brand,
                        Last4 = cardDetails.CardLast4 ?? "",
                        ExpiryMonth = cardDetails.ExpiryMonth ?? "",
                        ExpiryYear = cardDetails.ExpiryYear ?? "",
                        CustomerData = customerData
                    };
                }
                else
                {
                    throw new Exception($"Multi-use token creation failed: {response.ResponseMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Multi-use token creation error: {ex.Message}");
                throw;
            }
        });
    }
}