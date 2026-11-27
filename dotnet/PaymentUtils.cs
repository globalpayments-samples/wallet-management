using GlobalPayments.Api.Entities;
using GlobalPayments.Api.Entities.Enums;
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
    /// Uses a Verify to convert single-use to multi-use token, nothing is charged
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

                // First cardholder initiated use of a card saved on file
                var storedCredential = new StoredCredential
                {
                    Type = StoredCredentialType.Unscheduled,
                    Initiator = StoredCredentialInitiator.CardHolder,
                    Sequence = StoredCredentialSequence.First
                };

                // Verify the card and ask for a multi-use token, nothing is charged
                var response = card.Verify()
                    .WithCurrency("USD")
                    .WithRequestMultiUseToken(true)
                    .WithStoredCredential(storedCredential)
                    .WithAddress(address)
                    .Execute();

                if (response.ResponseCode == "SUCCESS" &&
                    response.ResponseMessage == "VERIFIED")
                {
                    // The .NET SDK leaves storage_mode off a Verify on a token, so GP API returns no
                    // multi-use token. Switch the verified token to multi-use instead.
                    var multiUseToken = response.Token;
                    if (string.IsNullOrEmpty(multiUseToken))
                    {
                        if (int.TryParse(cardDetails.ExpiryMonth, out var expMonth)) card.ExpMonth = expMonth;
                        if (int.TryParse(cardDetails.ExpiryYear, out var expYear)) card.ExpYear = expYear < 100 ? 2000 + expYear : expYear;

                        var update = card.UpdateToken()
                            .WithPaymentMethodUsageMode(PaymentMethodUsageMode.Multiple)
                            .Execute();
                        if (update.TokenUsageMode != PaymentMethodUsageMode.Multiple)
                        {
                            throw new Exception("Could not make the card token multi-use");
                        }
                        multiUseToken = paymentToken;
                    }

                    var brand = DetermineCardBrandFromType(cardDetails.CardType ?? "");

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