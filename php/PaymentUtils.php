<?php

declare(strict_types=1);

require_once 'vendor/autoload.php';

use Dotenv\Dotenv;
use GlobalPayments\Api\Entities\Address;
use GlobalPayments\Api\Entities\Customer;
use GlobalPayments\Api\Entities\Enums\Channel;
use GlobalPayments\Api\Entities\Enums\Environment;
use GlobalPayments\Api\Entities\Enums\TransactionStatus;
use GlobalPayments\Api\Entities\GpApi\AccessTokenInfo;
use GlobalPayments\Api\PaymentMethods\CreditCardData;
use GlobalPayments\Api\ServiceConfigs\Gateways\GpApiConfig;
use GlobalPayments\Api\ServicesContainer;

/**
 * Payment utility functions
 */
class PaymentUtils
{
    /**
     * Configure the Global Payments SDK (GP API)
     */
    public static function configureSdk(): void
    {
        $dotenv = Dotenv::createImmutable(__DIR__);
        $dotenv->load();

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

        ServicesContainer::configureService($config);
    }


    /**
     * Sanitize postal code by removing invalid characters
     */
    public static function sanitizePostalCode(?string $postalCode): string
    {
        if ($postalCode === null) {
            return '';
        }
        
        $sanitized = preg_replace('/[^a-zA-Z0-9-]/', '', $postalCode);
        return substr($sanitized, 0, 10);
    }

    /**
     * Determine card brand from Global Payments card type
     */
    public static function determineCardBrandFromType(string $cardType): string
    {
        $cardType = strtolower($cardType);
        
        switch ($cardType) {
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
    public static function createMultiUseTokenWithCustomer(string $paymentToken, array $customerData, array $cardDetails): array
    {
        try {
            // Create tokenized card from single-use token
            $tokenizedCard = new CreditCardData();
            $tokenizedCard->token = $paymentToken;
            $tokenizedCard->cardHolderName = trim($customerData['first_name'] ?? '') . ' ' . trim($customerData['last_name'] ?? '');

            // Create address from customer data
            $address = new Address();
            $address->streetAddress1 = trim($customerData['street_address'] ?? '');
            $address->city = trim($customerData['city'] ?? '');
            $address->province = trim($customerData['state'] ?? '');
            $address->postalCode = self::sanitizePostalCode($customerData['billing_zip'] ?? '');
            $address->country = trim($customerData['country'] ?? '');

            // Charge to convert single-use to multi-use token
            // GP API requires a charge (not verify) to create multi-use token
            $response = $tokenizedCard->charge(0.01) // Minimal verification amount
                ->withCurrency('USD')
                ->withRequestMultiUseToken(true)
                ->withAddress($address)
                ->execute();

            // Validate GP API response
            if ($response->responseCode === 'SUCCESS' &&
                $response->responseMessage === TransactionStatus::CAPTURED) {

                $brand = self::determineCardBrandFromType($cardDetails['cardType'] ?? '');
                $multiUseToken = $response->token ?? $paymentToken;

                return [
                    'multiUseToken' => $multiUseToken,
                    'brand' => $brand,
                    'last4' => $cardDetails['cardLast4'] ?? '',
                    'expiryMonth' => $cardDetails['expiryMonth'] ?? '',
                    'expiryYear' => $cardDetails['expiryYear'] ?? '',
                    'customerData' => $customerData
                ];
            } else {
                throw new \Exception('Multi-use token creation failed: ' . ($response->responseMessage ?? 'Unknown error'));
            }
        } catch (\Exception $e) {
            error_log('Multi-use token creation error: ' . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Get card details from stored payment token using Global Payments SDK
     */
    public static function getCardDetailsFromToken(string $storedPaymentToken): array
    {
        try {
            $card = new CreditCardData();
            $card->token = $storedPaymentToken;

            $response = $card->verify()
                ->withCurrency('USD')
                ->withRequestMultiUseToken(true)
                ->execute();

            if ($response->responseCode === '00') {
                $cardBrand = self::determineCardBrandFromType($response->cardType ?? '');
                $last4 = $response->cardLast4 ?? '';
                $expiryMonth = str_pad($response->cardExpMonth ?? '', 2, '0', STR_PAD_LEFT);
                $expiryYear = substr($response->cardExpYear ?? '', -2);

                return [
                    'brand' => $cardBrand,
                    'last4' => $last4,
                    'expiryMonth' => $expiryMonth,
                    'expiryYear' => $expiryYear,
                    'token' => $response->token ?? ''
                ];
            } else {
                throw new \Exception('Token verification failed: ' . ($response->responseMessage ?? 'Unknown error'));
            }
        } catch (\Exception $e) {
            error_log('SDK token lookup error: ' . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Process payment using Global Payments SDK (GP API)
     */
    public static function processPaymentWithSDK(string $storedPaymentToken, float $amount, string $currency): array
    {
        try {
            $card = new CreditCardData();
            $card->token = $storedPaymentToken;

            $response = $card->charge($amount)
                ->withCurrency($currency)
                ->execute();

            // Validate GP API response
            if ($response->responseCode === 'SUCCESS' &&
                $response->responseMessage === TransactionStatus::CAPTURED) {

                return [
                    'transaction_id' => $response->transactionId ?? 'txn_' . uniqid(),
                    'amount' => $amount,
                    'currency' => $currency,
                    'status' => 'approved',
                    'response_code' => $response->responseCode,
                    'response_message' => $response->responseMessage ?? 'Approved',
                    'timestamp' => date('c'),
                    'gateway_response' => [
                        'auth_code' => $response->authorizationCode ?? '',
                        'reference_number' => $response->referenceNumber ?? ''
                    ]
                ];
            } else {
                throw new \Exception('Payment failed: ' . ($response->responseMessage ?? 'Unknown error'));
            }
        } catch (\Exception $e) {
            error_log('SDK payment processing error: ' . $e->getMessage());
            throw $e;
        }
    }



    /**
     * Send success response
     */
    public static function sendSuccessResponse($data, string $message = 'Operation completed successfully'): void
    {
        http_response_code(200);
        
        $response = [
            'success' => true,
            'data' => $data,
            'message' => $message,
            'timestamp' => date('c')
        ];
        
        echo json_encode($response);
        exit();
    }

    /**
     * Send error response
     */
    public static function sendErrorResponse(int $statusCode, string $message, string $errorCode = null): void
    {
        http_response_code($statusCode);
        
        $response = [
            'success' => false,
            'message' => $message,
            'timestamp' => date('c')
        ];
        
        if ($errorCode) {
            $response['error_code'] = $errorCode;
        }
        
        echo json_encode($response);
        exit();
    }

    /**
     * Handle CORS headers
     */
    public static function handleCORS(): void
    {
        header('Access-Control-Allow-Origin: *');
        header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
        header('Access-Control-Allow-Headers: Content-Type, Authorization');
        header('Content-Type: application/json');
        
        if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
            http_response_code(200);
            exit();
        }
    }

    /**
     * Parse JSON input for POST requests
     */
    public static function parseJsonInput(): array
    {
        $inputData = [];
        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $rawInput = file_get_contents('php://input');
            if ($rawInput) {
                $inputData = json_decode($rawInput, true) ?? [];
            }
            $inputData = array_merge($_POST, $inputData);
        }
        return $inputData;
    }
}