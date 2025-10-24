package com.globalpayments.example;

import com.global.api.ServicesContainer;
import com.global.api.entities.Address;
import com.global.api.entities.Transaction;
import com.global.api.entities.enums.Channel;
import com.global.api.entities.enums.Environment;
import com.global.api.entities.exceptions.ApiException;
import com.global.api.entities.exceptions.ConfigurationException;
import com.global.api.paymentMethods.CreditCardData;
import com.global.api.serviceConfigs.GpApiConfig;
import com.global.api.services.GpApiService;
import io.github.cdimascio.dotenv.Dotenv;

import java.math.BigDecimal;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

/**
 * Payment utility functions for Global Payments SDK
 */
public class PaymentUtils {
    
    private static boolean sdkConfigured = false;
    private static final Dotenv dotenv = Dotenv.configure().ignoreIfMissing().load();
    
    /**
     * Configure the Global Payments SDK (GP API)
     */
    public static void configureSdk() throws ConfigurationException {
        if (!sdkConfigured) {
            String environment = dotenv.get("GP_API_ENVIRONMENT", "sandbox");
            boolean isProduction = "production".equalsIgnoreCase(environment);

            GpApiConfig config = new GpApiConfig();
            config.setAppId(dotenv.get("GP_API_APP_ID"));
            config.setAppKey(dotenv.get("GP_API_APP_KEY"));
            config.setEnvironment(isProduction ? Environment.PRODUCTION : Environment.TEST);
            config.setChannel(Channel.CardNotPresent);
            config.setCountry("US");

            ServicesContainer.configureService(config);
            sdkConfigured = true;
        }
    }
    
    /**
     * Sanitize postal code by removing invalid characters
     */
    public static String sanitizePostalCode(String postalCode) {
        if (postalCode == null) {
            return "";
        }
        
        String sanitized = postalCode.replaceAll("[^a-zA-Z0-9-]", "");
        return sanitized.length() > 10 ? sanitized.substring(0, 10) : sanitized;
    }
    
    public static String createStoredPaymentTokenWithSDK(Map<String, Object> data) throws Exception {
        try {
            CreditCardData card = new CreditCardData();
            card.setNumber((String) data.get("cardNumber"));
            card.setExpMonth(Integer.parseInt((String) data.get("expiryMonth")));
            card.setExpYear(Integer.parseInt((String) data.get("expiryYear")));
            card.setCvn((String) data.get("cvv"));
            
            @SuppressWarnings("unchecked")
            Map<String, String> billingAddress = (Map<String, String>) data.get("billingAddress");
            if (billingAddress != null) {
                Address address = new Address();
                address.setStreetAddress1(billingAddress.getOrDefault("street", ""));
                address.setCity(billingAddress.getOrDefault("city", ""));
                address.setState(billingAddress.getOrDefault("state", ""));
                address.setPostalCode(billingAddress.getOrDefault("zip", ""));
                address.setCountry(billingAddress.getOrDefault("country", "US"));
                card.setCardHolderName(billingAddress.getOrDefault("name", ""));
            }

            String token = card.tokenize();
            
            if (token != null && !token.isEmpty()) {
                return token;
            } else {
                throw new Exception("Tokenization failed: No token returned");
            }
        } catch (Exception e) {
            System.err.println("SDK tokenization error: " + e.getMessage());
            throw e;
        }
    }
    
    public static Map<String, String> getCardDetailsFromToken(String storedPaymentToken) throws Exception {
        try {
            CreditCardData card = new CreditCardData();
            card.setToken(storedPaymentToken);

            Transaction response = card.verify()
                    .withCurrency("USD")
                    .withRequestMultiUseToken(true)
                    .execute();

            if ("SUCCESS".equals(response.getResponseCode()) &&
                "CAPTURED".equals(response.getResponseMessage())) {
                String cardBrand = determineCardBrandFromType(response.getCardType() != null ? response.getCardType() : "");
                String last4 = response.getCardLast4() != null ? response.getCardLast4() : "";
                String expiryMonth = String.format("%02d", response.getCardExpMonth() > 0 ? response.getCardExpMonth() : 0);
                String expiryYear = String.format("%02d", response.getCardExpYear() > 0 ? response.getCardExpYear() % 100 : 0);
                
                System.out.println("🔍 Token lookup successful: " + cardBrand + " ending in " + last4);
                
                Map<String, String> cardDetails = new HashMap<>();
                cardDetails.put("brand", cardBrand);
                cardDetails.put("last4", last4);
                cardDetails.put("expiryMonth", expiryMonth);
                cardDetails.put("expiryYear", expiryYear);
                cardDetails.put("token", storedPaymentToken);
                
                return cardDetails;
            } else {
                throw new Exception("Token verification failed: " + (response.getResponseMessage() != null ? response.getResponseMessage() : "Unknown error"));
            }
        } catch (Exception e) {
            System.err.println("SDK token lookup error: " + e.getMessage());
            throw e;
        }
    }
    
    public static String determineCardBrandFromType(String cardType) {
        if (cardType == null) return "Unknown";
        
        String type = cardType.toLowerCase();
        
        switch (type) {
            case "visa":
                return "Visa";
            case "mastercard":
            case "mc":
                return "Mastercard";
            case "amex":
            case "americanexpress":
                return "American Express";
            case "discover":
                return "Discover";
            case "jcb":
                return "JCB";
            default:
                return "Unknown";
        }
    }
    
    public static Map<String, Object> processPaymentWithSDK(String storedPaymentToken, BigDecimal amount, String currency) throws Exception {
        try {
            CreditCardData card = new CreditCardData();
            card.setToken(storedPaymentToken);

            Transaction response = card.charge(amount)
                    .withCurrency(currency)
                    .execute();

            if ("SUCCESS".equals(response.getResponseCode()) &&
                "CAPTURED".equals(response.getResponseMessage())) {
                System.out.println("✅ Payment charged successfully: " + response.getTransactionId());
                
                Map<String, Object> result = new HashMap<>();
                result.put("transactionId", response.getTransactionId() != null ? response.getTransactionId() : "txn_" + UUID.randomUUID().toString());
                result.put("amount", amount);
                result.put("currency", currency);
                result.put("status", "approved");
                result.put("responseCode", response.getResponseCode());
                result.put("responseMessage", response.getResponseMessage() != null ? response.getResponseMessage() : "Approved");
                result.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
                
                Map<String, Object> gatewayResponse = new HashMap<>();
                gatewayResponse.put("authCode", response.getAuthorizationCode() != null ? response.getAuthorizationCode() : "");
                gatewayResponse.put("referenceNumber", response.getReferenceNumber() != null ? response.getReferenceNumber() : "");
                result.put("gatewayResponse", gatewayResponse);
                
                return result;
            } else {
                throw new Exception("Payment failed: " + (response.getResponseMessage() != null ? response.getResponseMessage() : "Unknown error"));
            }
        } catch (Exception e) {
            System.err.println("SDK payment processing error: " + e.getMessage());
            throw e;
        }
    }

    public static class CustomerData {
        public String firstName;
        public String lastName;
        public String email;
        public String phone;
        public String streetAddress;
        public String city;
        public String state;
        public String billingZip;
        public String country;

        public CustomerData(Map<String, String> data) {
            this.firstName = data.getOrDefault("first_name", "");
            this.lastName = data.getOrDefault("last_name", "");
            this.email = data.getOrDefault("email", "");
            this.phone = data.getOrDefault("phone", "");
            this.streetAddress = data.getOrDefault("street_address", "");
            this.city = data.getOrDefault("city", "");
            this.state = data.getOrDefault("state", "");
            this.billingZip = data.getOrDefault("billing_zip", "");
            this.country = data.getOrDefault("country", "");
        }
    }

    public static class CardDetails {
        public String cardType;
        public String cardLast4;
        public String expiryMonth;
        public String expiryYear;

        public CardDetails(Map<String, String> data) {
            this.cardType = data.getOrDefault("cardType", "");
            this.cardLast4 = data.getOrDefault("cardLast4", "");
            this.expiryMonth = data.getOrDefault("expiryMonth", "");
            this.expiryYear = data.getOrDefault("expiryYear", "");
        }
    }

    public static class MultiUseTokenResult {
        public String multiUseToken;
        public String brand;
        public String last4;
        public String expiryMonth;
        public String expiryYear;
        public CustomerData customerData;

        public MultiUseTokenResult(String token, String brand, String last4, String expiryMonth, String expiryYear, CustomerData customerData) {
            this.multiUseToken = token;
            this.brand = brand;
            this.last4 = last4;
            this.expiryMonth = expiryMonth;
            this.expiryYear = expiryYear;
            this.customerData = customerData;
        }
    }

    public static MultiUseTokenResult createMultiUseTokenWithCustomer(String paymentToken, CustomerData customerData, CardDetails cardDetails) throws Exception {
        try {
            CreditCardData card = new CreditCardData();
            card.setToken(paymentToken);
            card.setCardHolderName((customerData.firstName + " " + customerData.lastName).trim());

            Address address = new Address();
            address.setStreetAddress1(customerData.streetAddress.trim());
            address.setCity(customerData.city.trim());
            address.setState(customerData.state.trim());
            address.setPostalCode(sanitizePostalCode(customerData.billingZip));
            address.setCountry(customerData.country.trim());

            Transaction response = card.charge(new BigDecimal("0.01"))
                    .withCurrency("USD")
                    .withRequestMultiUseToken(true)
                    .withAddress(address)
                    .execute();

            if ("SUCCESS".equals(response.getResponseCode()) &&
                "CAPTURED".equals(response.getResponseMessage())) {
                String brand = determineCardBrandFromType(cardDetails.cardType);
                String finalToken = response.getToken() != null ? response.getToken() : paymentToken;

                System.out.println("✅ MULTI-USE TOKEN CREATION SUCCESS:");
                System.out.println("   ⏰ Timestamp: " + LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
                System.out.println("   🎯 Original Token: " + paymentToken.substring(0, Math.min(8, paymentToken.length())) + "...");
                System.out.println("   🔄 Multi-Use Token: " + finalToken.substring(0, Math.min(8, finalToken.length())) + "...");
                System.out.println("   💳 Card Brand: " + brand);
                System.out.println("   🔢 Last 4: " + cardDetails.cardLast4);
                System.out.println("   📅 Expiry: " + cardDetails.expiryMonth + "/" + cardDetails.expiryYear);
                System.out.println("   👤 Customer: " + customerData.firstName + " " + customerData.lastName);
                System.out.println("   📍 Address: " + customerData.city + ", " + customerData.state + " " + customerData.billingZip);

                return new MultiUseTokenResult(
                    finalToken,
                    brand,
                    cardDetails.cardLast4,
                    cardDetails.expiryMonth,
                    cardDetails.expiryYear,
                    customerData
                );
            } else {
                throw new Exception("Multi-use token creation failed: " + (response.getResponseMessage() != null ? response.getResponseMessage() : "Unknown error"));
            }
        } catch (Exception e) {
            System.err.println("Multi-use token creation error: " + e.getMessage());
            throw e;
        }
    }

}