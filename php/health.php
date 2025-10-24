<?php

declare(strict_types=1);

/**
 * Health Check Endpoint
 * 
 * GET /health - System health check
 */

require_once 'PaymentUtils.php';
require_once 'JsonStorage.php';

// Handle CORS
PaymentUtils::handleCORS();

// Initialize SDK
PaymentUtils::configureSdk();

// Only allow GET method
if ($_SERVER['REQUEST_METHOD'] !== 'GET') {
    PaymentUtils::sendErrorResponse(405, 'Method not allowed');
}

try {
    $paymentMethods = JsonStorage::readPaymentMethods();
    
    $response = [
        'status' => 'healthy',
        'version' => '1.0.0',
        'timestamp' => date('c'),
        'environment' => $_ENV['APP_ENV'] ?? 'development',
        'storage' => [
            'type' => 'json_file',
            'data_directory_exists' => is_dir(__DIR__ . '/data/'),
            'data_directory_writable' => is_writable(__DIR__ . '/data/') || !file_exists(__DIR__ . '/data/'),
            'payment_methods_count' => count($paymentMethods)
        ],
        'capabilities' => [
            'payment_method_creation' => true,
            'immediate_payments' => true,
            'delayed_charges' => true,
            'stored_payment_tokenization' => !empty($_ENV['GP_API_APP_KEY']),
            'mock_fallback' => true
        ],
        'endpoints' => [
            'GET /health' => 'System health check',
            'GET /config' => 'Get SDK configuration',
            'GET /payment-methods' => 'Get payment methods',
            'POST /payment-methods' => 'Create payment method',
            'POST /charge' => 'Process immediate charge ($25)',
            'GET /mock-mode' => 'Get mock mode status',
            'POST /mock-mode' => 'Toggle mock mode on/off'
        ]
    ];
    
    PaymentUtils::sendSuccessResponse($response, 'System is healthy and ready');

} catch (\Exception $e) {
    error_log('Health check error: ' . $e->getMessage());
    PaymentUtils::sendErrorResponse(500, 'Health check failed', 'SERVER_ERROR');
}