package com.globalpayments.example;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

/**
 * Mock responses for testing payment scenarios
 */
public class MockResponses {

    /**
     * Get successful payment response
     */
    public static Map<String, Object> getPaymentResponse(BigDecimal amount, String paymentMethodId) {
        Map<String, Object> response = new HashMap<>();
        response.put("transactionId", "txn_" + UUID.randomUUID().toString());
        response.put("amount", amount);
        response.put("currency", "USD");
        response.put("status", "approved");
        response.put("responseCode", "00");
        response.put("responseMessage", "Approved");
        response.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        
        Map<String, Object> gatewayResponse = new HashMap<>();
        gatewayResponse.put("authCode", "A" + String.format("%05d", (int)(Math.random() * 100000)));
        gatewayResponse.put("referenceNumber", "REF" + String.format("%010d", (int)(Math.random() * 1000000000)));
        response.put("gatewayResponse", gatewayResponse);
        
        return response;
    }
    
    
    /**
     * Get decline response with specific reason
     */
    public static Map<String, String> getDeclineResponse(String reason) {
        Map<String, String> declineReasons = new HashMap<>();
        declineReasons.put("insufficient_funds", "Insufficient Funds");
        declineReasons.put("generic", "Card Declined"); 
        declineReasons.put("pickup_card", "Pick Up Card");
        declineReasons.put("lost_card", "Lost Card");
        declineReasons.put("stolen_card", "Stolen Card");
        declineReasons.put("expired_card", "Expired Card");
        declineReasons.put("incorrect_cvc", "Incorrect CVC");
        declineReasons.put("incorrect_zip", "Incorrect ZIP");
        declineReasons.put("card_declined", "Card Declined");
        declineReasons.put("invalid_account", "Invalid Account");
        declineReasons.put("card_not_activated", "Card Not Activated");
        declineReasons.put("processing_error", "Processing Error");
        declineReasons.put("system_error", "System Error");
        
        Map<String, String> response = new HashMap<>();
        response.put("errorCode", reason.toUpperCase());
        response.put("responseMessage", declineReasons.getOrDefault(reason, "Card Declined"));
        
        return response;
    }
    
    /**
     * Generate mock stored payment token
     */
    public static String generateMockStoredPaymentToken() {
        return "token_" + UUID.randomUUID().toString().replace("-", "");
    }
    
    /**
     * Get card details from mock stored payment token
     */
    public static Map<String, String> getCardDetailsFromToken(String storedPaymentToken) {
        Map<String, String> mockDetails = new HashMap<>();
        
        // Default mock data
        mockDetails.put("brand", "Visa");
        mockDetails.put("last4", "0016");
        mockDetails.put("expiryMonth", "12");
        mockDetails.put("expiryYear", "28");

        // If token contains identifiable patterns, use them
        String tokenLower = storedPaymentToken.toLowerCase();
        if (tokenLower.contains("visa")) {
            mockDetails.put("brand", "Visa");
            mockDetails.put("last4", "0016");
        } else if (tokenLower.contains("mastercard") || tokenLower.contains("mc")) {
            mockDetails.put("brand", "Mastercard");
            mockDetails.put("last4", "5780");
        } else if (tokenLower.contains("amex")) {
            mockDetails.put("brand", "American Express");
            mockDetails.put("last4", "1018");
        } else if (tokenLower.contains("discover")) {
            mockDetails.put("brand", "Discover");
            mockDetails.put("last4", "6527");
        }

        return mockDetails;
    }
}