<?php

declare(strict_types=1);

/**
 * Mock Mode Configuration Class
 */
class MockModeConfig
{
    private static string $configFile = __DIR__ . '/data/mock_mode_config.json';

    /**
     * Get mock mode status
     */
    public static function isMockModeEnabled(): bool
    {
        if (!file_exists(self::$configFile)) {
            return false; // Default to disabled
        }
        
        $content = file_get_contents(self::$configFile);
        if ($content === false) {
            return false;
        }
        
        $config = json_decode($content, true);
        return $config['isEnabled'] ?? false;
    }

    /**
     * Set mock mode status
     */
    public static function setMockModeEnabled(bool $enabled): bool
    {
        // Ensure data directory exists
        $dataDir = dirname(self::$configFile);
        if (!is_dir($dataDir)) {
            mkdir($dataDir, 0755, true);
        }
        
        $config = [
            'isEnabled' => $enabled,
            'lastUpdated' => date('c')
        ];
        
        $json = json_encode($config, JSON_PRETTY_PRINT);
        return file_put_contents(self::$configFile, $json) !== false;
    }

    /**
     * Get mock mode status text
     */
    public static function getMockModeStatus(): string
    {
        return self::isMockModeEnabled() ? '🟡 ENABLED' : '🟢 DISABLED';
    }

    /**
     * Get mock mode text
     */
    public static function getMockModeText(): string
    {
        return self::isMockModeEnabled() ? 'enabled' : 'disabled';
    }

    /**
     * Get mock mode description
     */
    public static function getMockModeDescription(): string
    {
        return self::isMockModeEnabled() 
            ? 'Mock mode will be used for all operations' 
            : 'Live API will be attempted first';
    }
}

// Only execute endpoint logic if this file is accessed directly
if (basename($_SERVER['SCRIPT_NAME']) === 'mock-mode.php' || $_SERVER['REQUEST_URI'] === '/mock-mode') {
    require_once 'PaymentUtils.php';
    
    // Handle CORS
    PaymentUtils::handleCORS();
    
    $method = $_SERVER['REQUEST_METHOD'];

    try {
    if ($method === 'GET') {
        // Get mock mode status
        $isEnabled = MockModeConfig::isMockModeEnabled();
        
        $mockModeConfig = [
            'isEnabled' => $isEnabled
        ];
        
        $response = [
            'success' => true,
            'data' => $mockModeConfig,
            'message' => 'Mock mode is ' . MockModeConfig::getMockModeText(),
            'timestamp' => date('c')
        ];
        
        PaymentUtils::sendSuccessResponse($response['data'], $response['message']);
        
    } elseif ($method === 'POST') {
        // Toggle mock mode
        $data = PaymentUtils::parseJsonInput();
        
        if (!isset($data['isEnabled']) || !is_bool($data['isEnabled'])) {
            PaymentUtils::sendErrorResponse(400, 'Invalid JSON format: isEnabled field is required and must be boolean', 'VALIDATION_ERROR');
        }
        
        $isEnabled = $data['isEnabled'];
        
        if (!MockModeConfig::setMockModeEnabled($isEnabled)) {
            PaymentUtils::sendErrorResponse(500, 'Failed to update mock mode configuration', 'CONFIG_ERROR');
        }
        
        
        $mockModeConfig = [
            'isEnabled' => $isEnabled
        ];
        
        PaymentUtils::sendSuccessResponse($mockModeConfig, 'Mock mode ' . MockModeConfig::getMockModeText() . ' successfully');
        
    } else {
        PaymentUtils::sendErrorResponse(405, 'Method not allowed');
    }

    } catch (\Exception $e) {
        error_log('Mock mode error: ' . $e->getMessage());
        PaymentUtils::sendErrorResponse(500, 'Internal server error', 'SERVER_ERROR');
    }
}