<?php

declare(strict_types=1);

/**
 * Payment Methods Endpoint
 * 
 * GET /payment-methods - Retrieve saved payment methods
 * POST /payment-methods - Create new payment method (stored payment token) OR edit existing payment method
 *                         - Create: Requires storedPaymentToken (+ optional nickname, isDefault)
 *                         - Edit: Requires id (+ optional nickname, isDefault) - only nickname and default status can be edited
 */

require_once 'PaymentUtils.php';
require_once 'JsonStorage.php';
require_once 'MockResponses.php';
require_once 'mock-mode.php';

// Handle CORS
PaymentUtils::handleCORS();

// Initialize SDK
PaymentUtils::configureSdk();

$method = $_SERVER['REQUEST_METHOD'];

try {
    if ($method === 'GET') {
        // Get all payment methods
        $paymentMethods = JsonStorage::readPaymentMethods();
        
        $formattedMethods = array_map(function($method) {
            return [
                'id' => $method['id'],
                'type' => 'card',
                'last4' => $method['last4'],
                'brand' => $method['cardBrand'],
                'expiry' => $method['expiryMonth'] . '/' . $method['expiryYear'],
                'isDefault' => $method['isDefault'] ?? false,
                'nickname' => $method['nickname'] ?? ''
            ];
        }, $paymentMethods);
        
        PaymentUtils::sendSuccessResponse($formattedMethods, 'Payment methods retrieved successfully');
        
    } elseif ($method === 'POST') {
        $data = PaymentUtils::parseJsonInput();
        
        // Check if this is an edit operation (has 'id' field)
        if (!empty($data['id'])) {
            // Edit existing payment method
            $paymentMethodId = $data['id'];
            
            // Validate that the payment method exists
            if (!JsonStorage::paymentMethodExists($paymentMethodId)) {
                PaymentUtils::sendErrorResponse(404, 'Payment method not found', 'NOT_FOUND');
            }
            
            // Prepare update data (only editable fields)
            $updateData = [];
            if (isset($data['nickname'])) {
                $updateData['nickname'] = $data['nickname'];
            }
            if (isset($data['isDefault'])) {
                $updateData['isDefault'] = $data['isDefault'];
            }
            
            // Validate update data
            $validationErrors = JsonStorage::validateUpdateData($updateData);
            if (!empty($validationErrors)) {
                PaymentUtils::sendErrorResponse(400, implode(', ', $validationErrors), 'VALIDATION_ERROR');
            }
            
            // Update the payment method
            if (!JsonStorage::updatePaymentMethod($paymentMethodId, $updateData)) {
                PaymentUtils::sendErrorResponse(500, 'Failed to update payment method', 'UPDATE_ERROR');
            }
            
            // Get updated payment method for response
            $updatedMethod = JsonStorage::findPaymentMethod($paymentMethodId);
            
            $response = [
                'id' => $updatedMethod['id'],
                'type' => 'card',
                'last4' => $updatedMethod['last4'],
                'brand' => $updatedMethod['cardBrand'],
                'expiry' => $updatedMethod['expiryMonth'] . '/' . $updatedMethod['expiryYear'],
                'nickname' => $updatedMethod['nickname'] ?? '',
                'isDefault' => $updatedMethod['isDefault'] ?? false,
                'updatedAt' => $updatedMethod['updatedAt']
            ];
            
            PaymentUtils::sendSuccessResponse($response, 'Payment method updated successfully');
            
        } else {
            // Create a new payment method using payment_token from GP PaymentForm
            if (empty($data['payment_token'])) {
                PaymentUtils::sendErrorResponse(400, 'Missing required payment_token', 'VALIDATION_ERROR');
            }

            if (empty($data['cardDetails'])) {
                PaymentUtils::sendErrorResponse(400, 'Missing required cardDetails', 'VALIDATION_ERROR');
            }

            $paymentMethodId = JsonStorage::generateId();
            $paymentToken = $data['payment_token'];
            $cardDetails = $data['cardDetails'];
            $mockMode = MockModeConfig::isMockModeEnabled();

            // Extract customer data from request
            $customerData = [
                'first_name' => $data['first_name'] ?? '',
                'last_name' => $data['last_name'] ?? '',
                'email' => $data['email'] ?? '',
                'phone' => $data['phone'] ?? '',
                'street_address' => $data['street_address'] ?? '',
                'city' => $data['city'] ?? '',
                'state' => $data['state'] ?? '',
                'billing_zip' => $data['billing_zip'] ?? '',
                'country' => $data['country'] ?? ''
            ];

            // Create multi-use token with customer data or use mock
            $multiUseTokenData = null;
            $finalToken = $paymentToken;

            if (!$mockMode && !empty($_ENV['GP_API_APP_KEY']) && !empty($_ENV['GP_API_APP_KEY'])) {
                try {
                    $multiUseTokenData = PaymentUtils::createMultiUseTokenWithCustomer($paymentToken, $customerData, $cardDetails);
                    $finalToken = $multiUseTokenData['multiUseToken'];
                } catch (\Exception $e) {
                    error_log('Multi-use token creation error: ' . $e->getMessage());
                    // Fall back to mock mode if token creation fails
                    $mockMode = true;
                }
            }

            // Use mock data in mock mode or if token creation failed
            if ($mockMode || !$multiUseTokenData) {
                $brand = PaymentUtils::determineCardBrandFromType($cardDetails['cardType'] ?? '');
                $multiUseTokenData = [
                    'multiUseToken' => $paymentToken,
                    'brand' => $brand,
                    'last4' => $cardDetails['cardLast4'] ?? '',
                    'expiryMonth' => $cardDetails['expiryMonth'] ?? '',
                    'expiryYear' => $cardDetails['expiryYear'] ?? '',
                    'customerData' => $customerData
                ];
            }

            $validationData = [
                'cardBrand' => $multiUseTokenData['brand'],
                'last4' => $multiUseTokenData['last4'],
                'expiryMonth' => $multiUseTokenData['expiryMonth'],
                'expiryYear' => $multiUseTokenData['expiryYear']
            ];

            $validationErrors = JsonStorage::validatePaymentMethod($validationData);
            if (!empty($validationErrors)) {
                PaymentUtils::sendErrorResponse(400, implode(', ', $validationErrors), 'VALIDATION_ERROR');
            }

            $paymentMethod = [
                'id' => $paymentMethodId,
                'storedPaymentToken' => $finalToken,
                'cardBrand' => $multiUseTokenData['brand'],
                'last4' => $multiUseTokenData['last4'],
                'expiryMonth' => $multiUseTokenData['expiryMonth'],
                'expiryYear' => $multiUseTokenData['expiryYear'],
                'nickname' => $data['nickname'] ?? ($multiUseTokenData['brand'] . ' ending in ' . $multiUseTokenData['last4']),
                'isDefault' => $data['isDefault'] ?? false,
                'customerData' => $customerData
            ];

            if (!JsonStorage::addPaymentMethod($paymentMethod)) {
                PaymentUtils::sendErrorResponse(500, 'Failed to save payment method', 'STORAGE_ERROR');
            }

            $response = [
                'id' => $paymentMethodId,
                'storedPaymentToken' => $finalToken,
                'type' => 'card',
                'last4' => $multiUseTokenData['last4'],
                'brand' => $multiUseTokenData['brand'],
                'expiry' => $multiUseTokenData['expiryMonth'] . '/' . $multiUseTokenData['expiryYear'],
                'nickname' => $paymentMethod['nickname'],
                'isDefault' => $paymentMethod['isDefault'],
                'mockMode' => $mockMode
            ];

            PaymentUtils::sendSuccessResponse($response, 'Payment method created and saved successfully');
        }
        
    } else {
        PaymentUtils::sendErrorResponse(405, 'Method not allowed');
    }

} catch (\Exception $e) {
    error_log('Payment methods error: ' . $e->getMessage());
    PaymentUtils::sendErrorResponse(500, 'Internal server error', 'SERVER_ERROR');
}