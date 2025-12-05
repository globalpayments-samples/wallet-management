<?php

declare(strict_types=1);

/**
 * JSON Storage utility class for payment methods
 */
class JsonStorage
{
    private static string $dataDir = __DIR__ . '/data';
    private static string $paymentMethodsFile = 'payment_methods.json';

    /**
     * Initialize storage directory
     */
    public static function init(): void
    {
        if (!is_dir(self::$dataDir)) {
            mkdir(self::$dataDir, 0755, true);
        }
    }

    /**
     * Generate unique ID for payment methods
     */
    public static function generateId(): string
    {
        return 'pm_' . uniqid() . '_' . bin2hex(random_bytes(8));
    }

    /**
     * Read all payment methods
     */
    public static function readPaymentMethods(): array
    {
        $filePath = self::$dataDir . '/' . self::$paymentMethodsFile;
        
        if (!file_exists($filePath)) {
            return [];
        }
        
        $content = file_get_contents($filePath);
        if ($content === false) {
            return [];
        }
        
        $data = json_decode($content, true);
        return $data ?? [];
    }

    /**
     * Write payment methods to storage
     */
    public static function writePaymentMethods(array $methods): bool
    {
        self::init();
        $filePath = self::$dataDir . '/' . self::$paymentMethodsFile;
        
        $json = json_encode($methods, JSON_PRETTY_PRINT);
        return file_put_contents($filePath, $json) !== false;
    }

    /**
     * Add a new payment method
     */
    public static function addPaymentMethod(array $paymentMethod): bool
    {
        $paymentMethod['createdAt'] = date('c');
        $paymentMethod['updatedAt'] = date('c');
        
        $methods = self::readPaymentMethods();
        $methods[] = $paymentMethod;
        
        return self::writePaymentMethods($methods);
    }

    /**
     * Find payment method by ID
     */
    public static function findPaymentMethod(string $id): ?array
    {
        $methods = self::readPaymentMethods();
        
        foreach ($methods as $method) {
            if ($method['id'] === $id) {
                return $method;
            }
        }
        
        return null;
    }

    /**
     * Update an existing payment method
     */
    public static function updatePaymentMethod(string $id, array $updateData): bool
    {
        $methods = self::readPaymentMethods();
        $updated = false;
        
        foreach ($methods as $index => $method) {
            if ($method['id'] === $id) {
                // Update only allowed fields
                if (isset($updateData['nickname'])) {
                    $methods[$index]['nickname'] = $updateData['nickname'];
                }
                
                if (isset($updateData['isDefault'])) {
                    // If setting this as default, unset default from all others
                    if ($updateData['isDefault']) {
                        foreach ($methods as $i => $m) {
                            $methods[$i]['isDefault'] = false;
                        }
                    }
                    $methods[$index]['isDefault'] = $updateData['isDefault'];
                }
                
                // Update timestamp
                $methods[$index]['updatedAt'] = date('c');
                $updated = true;
                break;
            }
        }
        
        if ($updated) {
            return self::writePaymentMethods($methods);
        }
        
        return false;
    }

    /**
     * Check if a payment method exists by ID
     */
    public static function paymentMethodExists(string $id): bool
    {
        return self::findPaymentMethod($id) !== null;
    }

    /**
     * Validate payment method data
     */
    public static function validatePaymentMethod(array $data): array
    {
        $errors = [];
        
        if (empty($data['cardBrand'])) {
            $errors[] = 'Card brand is required';
        }
        
        if (empty($data['last4']) || strlen($data['last4']) !== 4) {
            $errors[] = 'Valid last 4 digits are required';
        }
        
        if (empty($data['expiryMonth']) || !is_numeric($data['expiryMonth']) || 
            $data['expiryMonth'] < 1 || $data['expiryMonth'] > 12) {
            $errors[] = 'Valid expiry month is required';
        }
        
        if (empty($data['expiryYear']) || !is_numeric($data['expiryYear'])) {
            $errors[] = 'Valid expiry year is required';
        }
        
        return $errors;
    }

    /**
     * Validate update data for payment method editing
     */
    public static function validateUpdateData(array $data): array
    {
        $errors = [];
        
        // Nickname validation (optional)
        if (isset($data['nickname']) && strlen($data['nickname']) > 100) {
            $errors[] = 'Nickname must be 100 characters or less';
        }
        
        // isDefault validation (optional but must be boolean if provided)
        if (isset($data['isDefault']) && !is_bool($data['isDefault'])) {
            $errors[] = 'isDefault must be a boolean value';
        }
        
        return $errors;
    }
}