namespace CardPaymentSample;

/// <summary>
/// Mock responses for testing payment scenarios
/// </summary>
public static class MockResponses
{
    /// <summary>
    /// Get successful payment response
    /// </summary>
    public static PaymentResponse GetPaymentResponse(decimal amount, string paymentMethodId)
    {
        var random = new Random();
        
        return new PaymentResponse
        {
            TransactionId = $"txn_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString()[^9..]}",
            Amount = amount,
            Currency = "USD",
            Status = "approved",
            ResponseCode = "00",
            ResponseMessage = "Approved",
            Timestamp = DateTime.UtcNow,
            GatewayResponse = new GatewayResponse
            {
                AuthCode = $"A{random.Next(10000, 99999):D5}",
                ReferenceNumber = $"REF{random.Next(100000000, 999999999):D10}"
            }
        };
    }


    /// <summary>
    /// Get decline response with specific reason
    /// </summary>
    public static DeclineResponse GetDeclineResponse(string reason)
    {
        var declineReasons = new Dictionary<string, string>
        {
            {"insufficient_funds", "Insufficient Funds"},
            {"generic", "Card Declined"},
            {"pickup_card", "Pick Up Card"},
            {"lost_card", "Lost Card"},
            {"stolen_card", "Stolen Card"},
            {"expired_card", "Expired Card"},
            {"incorrect_cvc", "Incorrect CVC"},
            {"incorrect_zip", "Incorrect ZIP"},
            {"card_declined", "Card Declined"},
            {"invalid_account", "Invalid Account"},
            {"card_not_activated", "Card Not Activated"},
            {"processing_error", "Processing Error"},
            {"system_error", "System Error"}
        };

        var message = declineReasons.TryGetValue(reason, out var msg) ? msg : "Card Declined";

        return new DeclineResponse
        {
            ErrorCode = reason.ToUpper(),
            ResponseMessage = message
        };
    }

    /// <summary>
    /// Generate mock stored payment token
    /// </summary>
    public static string GenerateMockStoredPaymentToken()
    {
        return $"token_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString().Replace("-", "")[..16]}";
    }
}