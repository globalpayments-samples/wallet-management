<?php

declare(strict_types=1);

/**
 * API Router for Wallet Management System
 *
 * This script routes requests to individual endpoint files:
 * - /health -> health.php
 * - /config -> config.php
 * - /payment-methods -> payment-methods.php
 * - /mock-mode -> mock-mode.php
 *
 * PHP version 7.4 or higher
 *
 * @category  Payment_Processing
 * @package   WalletManagement
 * @author    Global Payments
 * @license   MIT License
 * @link      https://github.com/globalpayments
 */

ini_set('display_errors', '0');

// Get the requested path
$method = $_SERVER['REQUEST_METHOD'];
$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);

// Remove leading slash and normalize path
$path = ltrim($path, '/');

// Handle CORS for all requests
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');
header('Content-Type: application/json');

if ($method === 'OPTIONS') {
    http_response_code(200);
    exit();
}

// Route to appropriate endpoint file
switch ($path) {
    case 'health':
        require_once 'health.php';
        break;

    case 'config':
        require_once 'config.php';
        break;

    case 'payment-methods':
        require_once 'payment-methods.php';
        break;

    case 'mock-mode':
        require_once 'mock-mode.php';
        break;
        
    default:
        http_response_code(404);
        echo json_encode([
            'success' => false, 
            'message' => 'Endpoint not found',
            'available_endpoints' => [
                'GET /health' => 'System health check',
                'GET /config' => 'Get SDK configuration',
                'GET /payment-methods' => 'Get payment methods',
                'POST /payment-methods' => 'Create payment method',
                'GET /mock-mode' => 'Get mock mode status',
                'POST /mock-mode' => 'Toggle mock mode on/off'
            ]
        ]);
}
