<?php

declare(strict_types=1);

/**
 * Mock responses for testing without SDK
 */
class MockResponses
{
    /**
     * Generate mock stored payment token response
     */
    public static function getStoredPaymentToken(array $cardData): array
    {
        return [
            'id' => 'stored_' . uniqid() . '_' . bin2hex(random_bytes(8)),
            'brand' => $cardData['brand'],
            'last4' => $cardData['last4'],
            'exp_month' => $cardData['exp_month'],
            'exp_year' => $cardData['exp_year'],
            'created_at' => date('c'),
            'type' => 'card'
        ];
    }

    /**
     * Get card details from mock stored payment token
     */
    public static function getCardDetailsFromToken(string $storedPaymentToken): array
    {
        // Extract mock data from token pattern or use defaults for demo
        $mockDetails = [
            'brand' => 'Visa',
            'last4' => '0016',
            'expiryMonth' => '12',
            'expiryYear' => '28'
        ];

        // If token contains identifiable patterns, use them
        if (strpos($storedPaymentToken, 'visa') !== false) {
            $mockDetails['brand'] = 'Visa';
            $mockDetails['last4'] = '0016';
        } elseif (strpos($storedPaymentToken, 'mastercard') !== false || strpos($storedPaymentToken, 'mc') !== false) {
            $mockDetails['brand'] = 'Mastercard';
            $mockDetails['last4'] = '5780';
        } elseif (strpos($storedPaymentToken, 'amex') !== false) {
            $mockDetails['brand'] = 'American Express';
            $mockDetails['last4'] = '1018';
        } elseif (strpos($storedPaymentToken, 'discover') !== false) {
            $mockDetails['brand'] = 'Discover';
            $mockDetails['last4'] = '6527';
        }

        return $mockDetails;
    }

    /**
     * Generate mock payment response
     */
    public static function getPaymentResponse(float $amount, string $paymentMethodId): array
    {
        return [
            'transaction_id' => 'txn_' . uniqid(),
            'amount' => $amount,
            'currency' => 'USD',
            'status' => 'approved',
            'response_code' => '00',
            'response_message' => 'Approved',
            'timestamp' => date('c'),
            'payment_method_id' => $paymentMethodId,
            'gateway_response' => [
                'auth_code' => strtoupper(bin2hex(random_bytes(3))),
                'reference_number' => 'ref_' . uniqid()
            ]
        ];
    }



    /**
     * Generate decline responses for testing
     */
    public static function getDeclineResponse(string $reason): array
    {
        $responses = [
            'insufficient_funds' => [
                'response_code' => '51',
                'response_message' => 'Insufficient funds',
                'error_code' => 'CARD_DECLINED'
            ],
            'expired_card' => [
                'response_code' => '54',
                'response_message' => 'Expired card',
                'error_code' => 'EXPIRED_CARD'
            ],
            'invalid_card' => [
                'response_code' => '14',
                'response_message' => 'Invalid card number',
                'error_code' => 'INVALID_CARD'
            ]
        ];

        return $responses[$reason] ?? [
            'response_code' => '05',
            'response_message' => 'Do not honor',
            'error_code' => 'GENERIC_DECLINE'
        ];
    }
}