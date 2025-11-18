/**
 * JSON Storage utility class for payment methods
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import crypto from 'crypto';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DATA_DIR = path.join(__dirname, 'data');
const PAYMENT_METHODS_FILE = 'payment_methods.json';

/**
 * Initialize storage directory
 */
export function init() {
    if (!fs.existsSync(DATA_DIR)) {
        fs.mkdirSync(DATA_DIR, { recursive: true, mode: 0o755 });
    }
}

/**
 * Generate unique ID for payment methods
 */
export function generateId() {
    const uniquePart = Date.now().toString(36);
    const randomPart = crypto.randomBytes(8).toString('hex');
    return `pm_${uniquePart}_${randomPart}`;
}

/**
 * Read all payment methods
 */
export function readPaymentMethods() {
    const filePath = path.join(DATA_DIR, PAYMENT_METHODS_FILE);

    if (!fs.existsSync(filePath)) {
        return [];
    }

    try {
        const content = fs.readFileSync(filePath, 'utf8');
        const data = JSON.parse(content);
        return data || [];
    } catch (error) {
        console.error('Error reading payment methods:', error.message);
        return [];
    }
}

/**
 * Write payment methods to storage
 */
export function writePaymentMethods(methods) {
    init();
    const filePath = path.join(DATA_DIR, PAYMENT_METHODS_FILE);

    try {
        const json = JSON.stringify(methods, null, 2);
        fs.writeFileSync(filePath, json, 'utf8');
        return true;
    } catch (error) {
        console.error('Error writing payment methods:', error.message);
        return false;
    }
}

/**
 * Add a new payment method
 */
export function addPaymentMethod(paymentMethod) {
    paymentMethod.createdAt = new Date().toISOString();
    paymentMethod.updatedAt = new Date().toISOString();

    const methods = readPaymentMethods();
    methods.push(paymentMethod);

    return writePaymentMethods(methods);
}

/**
 * Find payment method by ID
 */
export function findPaymentMethod(id) {
    const methods = readPaymentMethods();

    for (const method of methods) {
        if (method.id === id) {
            return method;
        }
    }

    return null;
}

/**
 * Update an existing payment method
 */
export function updatePaymentMethod(id, updateData) {
    const methods = readPaymentMethods();
    let updated = false;

    for (let i = 0; i < methods.length; i++) {
        if (methods[i].id === id) {
            // Update only allowed fields
            if (updateData.nickname !== undefined) {
                methods[i].nickname = updateData.nickname;
            }

            if (updateData.isDefault !== undefined) {
                // If setting this as default, unset default from all others
                if (updateData.isDefault) {
                    for (let j = 0; j < methods.length; j++) {
                        methods[j].isDefault = false;
                    }
                }
                methods[i].isDefault = updateData.isDefault;
            }

            // Update timestamp
            methods[i].updatedAt = new Date().toISOString();
            updated = true;
            break;
        }
    }

    if (updated) {
        return writePaymentMethods(methods);
    }

    return false;
}

/**
 * Check if a payment method exists by ID
 */
export function paymentMethodExists(id) {
    return findPaymentMethod(id) !== null;
}

/**
 * Validate payment method data
 */
export function validatePaymentMethod(data) {
    const errors = [];

    if (!data.cardBrand) {
        errors.push('Card brand is required');
    }

    if (!data.last4 || data.last4.length !== 4) {
        errors.push('Valid last 4 digits are required');
    }

    if (!data.expiryMonth || isNaN(data.expiryMonth) ||
        data.expiryMonth < 1 || data.expiryMonth > 12) {
        errors.push('Valid expiry month is required');
    }

    if (!data.expiryYear || isNaN(data.expiryYear)) {
        errors.push('Valid expiry year is required');
    }

    return errors;
}

/**
 * Validate update data for payment method editing
 */
export function validateUpdateData(data) {
    const errors = [];

    // Nickname validation (optional)
    if (data.nickname !== undefined && data.nickname.length > 100) {
        errors.push('Nickname must be 100 characters or less');
    }

    // isDefault validation (optional but must be boolean if provided)
    if (data.isDefault !== undefined && typeof data.isDefault !== 'boolean') {
        errors.push('isDefault must be a boolean value');
    }

    return errors;
}
