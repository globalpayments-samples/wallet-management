<?php

declare(strict_types=1);

/**
 * Configuration Endpoint - GP API
 *
 * This script provides configuration information for the client-side SDK,
 * including GP API access token for frontend tokenization.
 *
 * PHP version 7.4 or higher
 *
 * @category  Configuration
 * @package   GlobalPayments_WalletManagement
 * @author    Global Payments
 * @license   MIT License
 * @link      https://github.com/globalpayments
 */

require_once 'vendor/autoload.php';

use Dotenv\Dotenv;
use GlobalPayments\Api\ServiceConfigs\Gateways\GpApiConfig;
use GlobalPayments\Api\ServicesContainer;
use GlobalPayments\Api\Entities\Enums\Environment;
use GlobalPayments\Api\Entities\Enums\Channel;
use GlobalPayments\Api\Entities\GpApi\AccessTokenInfo;
use GlobalPayments\Api\Services\GpApiService;

// Load environment variables
$dotenv = Dotenv::createImmutable(__DIR__);
$dotenv->load();

// Set response headers
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

// Handle preflight requests
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

// Only allow GET method
if ($_SERVER['REQUEST_METHOD'] !== 'GET') {
    http_response_code(405);
    echo json_encode([
        'success' => false,
        'message' => 'Method not allowed'
    ]);
    exit;
}

try {
    // Configure GP API to generate access token for client-side use
    $config = new GpApiConfig();
    $config->appId = $_ENV['GP_API_APP_ID'] ?? '';
    $config->appKey = $_ENV['GP_API_APP_KEY'] ?? '';
    $config->environment = Environment::TEST;
    $config->channel = Channel::CardNotPresent;
    $config->country = 'US';

    // Configure AccessTokenInfo with transaction processing account details
    $accessTokenInfo = new AccessTokenInfo();
    $accessTokenInfo->transactionProcessingAccountName = 'transaction_processing';
    $accessTokenInfo->riskAssessmentAccountName = 'EOS_RiskAssessment';
    $config->accessTokenInfo = $accessTokenInfo;

    // Set permissions specifically for client-side single-use tokenization
    $config->permissions = ['PMT_POST_Create_Single'];

    // Configure service to establish connection
    ServicesContainer::configureService($config);

    // Generate session token for client-side tokenization
    $sessionToken = GpApiService::generateTransactionKey($config);

    if (is_object($sessionToken) && isset($sessionToken->accessToken)) {
        $accessToken = $sessionToken->accessToken;
        error_log('Session token generated successfully: ' . substr($accessToken, 0, 8) . '...');
    } else {
        throw new Exception('Invalid session token response format');
    }

    if (empty($accessToken)) {
        throw new Exception('Failed to generate session token');
    }

    // Return configuration
    http_response_code(200);
    echo json_encode([
        'success' => true,
        'data' => [
            'accessToken' => $accessToken
        ],
        'message' => 'Configuration retrieved successfully',
        'timestamp' => date('c')
    ]);

} catch (Exception $e) {
    error_log('Configuration error: ' . $e->getMessage());
    http_response_code(500);
    echo json_encode([
        'success' => false,
        'message' => 'Error loading configuration: ' . $e->getMessage(),
        'error_code' => 'CONFIG_ERROR',
        'timestamp' => date('c')
    ]);
}
