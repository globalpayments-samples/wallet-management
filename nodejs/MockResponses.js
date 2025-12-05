/**
 * Mock responses for testing without SDK
 */

import crypto from 'crypto';

/**
 * Generate mock stored payment token response
 */
export function getStoredPaymentToken(cardData) {
    const uniquePart = Date.now().toString(36);
    const randomPart = crypto.randomBytes(8).toString('hex');

    return {
        id: `stored_${uniquePart}_${randomPart}`,
        brand: cardData.brand,
        last4: cardData.last4,
        exp_month: cardData.exp_month,
        exp_year: cardData.exp_year,
        created_at: new Date().toISOString(),
        type: 'card'
    };
}

/**
 * Get card details from mock stored payment token
 */
export function getCardDetailsFromToken(storedPaymentToken) {
    // Extract mock data from token pattern or use defaults for demo
    const mockDetails = {
        brand: 'Visa',
        last4: '0016',
        expiryMonth: '12',
        expiryYear: '28'
    };

    // If token contains identifiable patterns, use them
    const tokenLower = storedPaymentToken.toLowerCase();
    if (tokenLower.includes('visa')) {
        mockDetails.brand = 'Visa';
        mockDetails.last4 = '0016';
    } else if (tokenLower.includes('mastercard') || tokenLower.includes('mc')) {
        mockDetails.brand = 'Mastercard';
        mockDetails.last4 = '5780';
    } else if (tokenLower.includes('amex')) {
        mockDetails.brand = 'American Express';
        mockDetails.last4 = '1018';
    } else if (tokenLower.includes('discover')) {
        mockDetails.brand = 'Discover';
        mockDetails.last4 = '6527';
    }

    return mockDetails;
}

/**
 * Generate mock payment response
 */
export function getPaymentResponse(amount, paymentMethodId) {
    return {
        transaction_id: `txn_${Date.now()}`,
        amount: amount,
        currency: 'USD',
        status: 'approved',
        response_code: '00',
        response_message: 'Approved',
        timestamp: new Date().toISOString(),
        payment_method_id: paymentMethodId,
        gateway_response: {
            auth_code: crypto.randomBytes(3).toString('hex').toUpperCase(),
            reference_number: `ref_${Date.now()}`
        }
    };
}

/**
 * Generate decline responses for testing
 */
export function getDeclineResponse(reason) {
    const responses = {
        insufficient_funds: {
            response_code: '51',
            response_message: 'Insufficient funds',
            error_code: 'CARD_DECLINED'
        },
        expired_card: {
            response_code: '54',
            response_message: 'Expired card',
            error_code: 'EXPIRED_CARD'
        },
        invalid_card: {
            response_code: '14',
            response_message: 'Invalid card number',
            error_code: 'INVALID_CARD'
        }
    };

    return responses[reason] || {
        response_code: '05',
        response_message: 'Do not honor',
        error_code: 'GENERIC_DECLINE'
    };
}
