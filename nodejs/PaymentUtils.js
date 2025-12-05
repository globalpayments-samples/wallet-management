/**
 * Payment utility functions for Global Payments SDK
 */

import * as dotenv from 'dotenv';
import {
    ServicesContainer,
    GpApiConfig,
    Address,
    CreditCardData,
    Environment,
    Channel,
    AccessTokenInfo,
    TransactionStatus
} from 'globalpayments-api';

// Load environment variables
dotenv.config();

/**
 * Configure the Global Payments SDK (GP API)
 */
export function configureSdk() {
    const config = new GpApiConfig();
    config.appId = process.env.GP_API_APP_ID || '';
    config.appKey = process.env.GP_API_APP_KEY || '';
    config.environment = Environment.TEST;
    config.channel = Channel.CardNotPresent;
    config.country = 'US';

    // Configure AccessTokenInfo with transaction processing account details
    const accessTokenInfo = new AccessTokenInfo();
    accessTokenInfo.transactionProcessingAccountName = 'transaction_processing';
    accessTokenInfo.riskAssessmentAccountName = 'EOS_RiskAssessment';
    config.accessTokenInfo = accessTokenInfo;

    ServicesContainer.configureService(config);
}

/**
 * Sanitize postal code by removing invalid characters
 */
export function sanitizePostalCode(postalCode) {
    if (!postalCode) {
        return '';
    }

    const sanitized = postalCode.replace(/[^a-zA-Z0-9-]/g, '');
    return sanitized.substring(0, 10);
}

/**
 * Determine card brand from Global Payments card type
 */
export function determineCardBrandFromType(cardType) {
    const type = cardType.toLowerCase();

    switch (type) {
        case 'visa':
            return 'Visa';
        case 'mastercard':
        case 'mc':
            return 'Mastercard';
        case 'amex':
        case 'americanexpress':
            return 'American Express';
        case 'discover':
            return 'Discover';
        case 'jcb':
            return 'JCB';
        default:
            return 'Unknown';
    }
}

/**
 * Create multi-use token with customer data attached (GP API)
 * Uses charge-based approach to convert single-use to multi-use token
 */
export async function createMultiUseTokenWithCustomer(paymentToken, customerData, cardDetails) {
    try {
        // Create tokenized card from single-use token
        const tokenizedCard = new CreditCardData();
        tokenizedCard.token = paymentToken;
        tokenizedCard.cardHolderName = `${customerData.first_name || ''} ${customerData.last_name || ''}`.trim();

        // Create address from customer data
        const address = new Address();
        address.streetAddress1 = customerData.street_address || '';
        address.city = customerData.city || '';
        address.province = customerData.state || '';
        address.postalCode = sanitizePostalCode(customerData.billing_zip || '');
        address.country = customerData.country || '';

        // Charge to convert single-use to multi-use token
        // GP API requires a charge (not verify) to create multi-use token
        const response = await tokenizedCard.charge(0.01) // Minimal verification amount
            .withCurrency('USD')
            .withRequestMultiUseToken(true)
            .withAddress(address)
            .execute();

        // Validate GP API response
        if (response.responseCode === 'SUCCESS' &&
            response.responseMessage === TransactionStatus.CAPTURED) {

            const brand = determineCardBrandFromType(cardDetails.cardType || '');
            const multiUseToken = response.token || paymentToken;

            console.log('Multi-use token created successfully: ' + multiUseToken.substring(0, 8) + '...');

            return {
                multiUseToken: multiUseToken,
                brand: brand,
                last4: cardDetails.cardLast4 || '',
                expiryMonth: cardDetails.expiryMonth || '',
                expiryYear: cardDetails.expiryYear || '',
                customerData: customerData
            };
        } else {
            throw new Error('Multi-use token creation failed: ' + (response.responseMessage || 'Unknown error'));
        }
    } catch (error) {
        console.error('Multi-use token creation error: ' + error.message);
        throw error;
    }
}

/**
 * Get card details from stored payment token using Global Payments SDK
 */
export async function getCardDetailsFromToken(storedPaymentToken) {
    try {
        const card = new CreditCardData();
        card.token = storedPaymentToken;

        const response = await card.verify()
            .withCurrency('USD')
            .withRequestMultiUseToken(true)
            .execute();

        if (response.responseCode === '00') {
            const cardBrand = determineCardBrandFromType(response.cardType || '');
            const last4 = response.cardLast4 || '';
            const expiryMonth = String(response.cardExpMonth || '').padStart(2, '0');
            const expiryYear = String(response.cardExpYear || '').slice(-2);

            return {
                brand: cardBrand,
                last4: last4,
                expiryMonth: expiryMonth,
                expiryYear: expiryYear,
                token: response.token || ''
            };
        } else {
            throw new Error('Token verification failed: ' + (response.responseMessage || 'Unknown error'));
        }
    } catch (error) {
        console.error('SDK token lookup error: ' + error.message);
        throw error;
    }
}

/**
 * Process payment using Global Payments SDK (GP API)
 */
export async function processPaymentWithSDK(storedPaymentToken, amount, currency) {
    try {
        const card = new CreditCardData();
        card.token = storedPaymentToken;

        const response = await card.charge(amount)
            .withCurrency(currency)
            .execute();

        // Validate GP API response
        if (response.responseCode === 'SUCCESS' &&
            response.responseMessage === TransactionStatus.CAPTURED) {

            return {
                transaction_id: response.transactionId || 'txn_' + Date.now(),
                amount: amount,
                currency: currency,
                status: 'approved',
                response_code: response.responseCode,
                response_message: response.responseMessage || 'Approved',
                timestamp: new Date().toISOString(),
                gateway_response: {
                    auth_code: response.authorizationCode || '',
                    reference_number: response.referenceNumber || ''
                }
            };
        } else {
            throw new Error('Payment failed: ' + (response.responseMessage || 'Unknown error'));
        }
    } catch (error) {
        console.error('SDK payment processing error: ' + error.message);
        throw error;
    }
}

/**
 * Send success response
 */
export function sendSuccessResponse(res, data, message = 'Operation completed successfully') {
    res.status(200).json({
        success: true,
        data: data,
        message: message,
        timestamp: new Date().toISOString()
    });
}

/**
 * Send error response
 */
export function sendErrorResponse(res, statusCode, message, errorCode = null) {
    const response = {
        success: false,
        message: message,
        timestamp: new Date().toISOString()
    };

    if (errorCode) {
        response.error_code = errorCode;
    }

    res.status(statusCode).json(response);
}

/**
 * Handle CORS headers middleware
 */
export function handleCORS(req, res, next) {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Content-Type, Authorization');
    res.header('Content-Type', 'application/json');

    if (req.method === 'OPTIONS') {
        return res.status(200).end();
    }

    next();
}
