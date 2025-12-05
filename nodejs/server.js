/**
 * Global Payments Wallet Management - Node.js
 *
 * This Express application provides wallet management functionality
 * with multi-use token creation and customer data integration.
 */

import express from 'express';
import * as dotenv from 'dotenv';
import {
    GpApiConfig,
    ServicesContainer,
    Environment,
    Channel,
    AccessTokenInfo,
    GpApiService
} from 'globalpayments-api';

import * as PaymentUtils from './PaymentUtils.js';
import * as JsonStorage from './JsonStorage.js';
import * as MockResponses from './MockResponses.js';
import * as MockModeConfig from './MockModeConfig.js';

// Load environment variables from .env file
dotenv.config();

/**
 * Initialize Express application with necessary middleware
 */
const app = express();
const port = process.env.PORT || 8000;

app.use(express.static('.')); // Serve static files
app.use(express.urlencoded({ extended: true })); // Parse form data
app.use(express.json()); // Parse JSON requests

// Configure Global Payments SDK
PaymentUtils.configureSdk();

/**
 * Config endpoint - provides GP API access token for client-side tokenization
 */
app.get('/config', async (req, res) => {
    try {
        // Configure GP API to generate access token for client-side use
        const config = new GpApiConfig();
        config.appId = process.env.GP_API_APP_ID || '';
        config.appKey = process.env.GP_API_APP_KEY || '';
        config.environment = Environment.TEST;
        config.channel = Channel.CardNotPresent;
        config.country = 'US';

        // Configure AccessTokenInfo with transaction processing account details
        const accessTokenInfo = new AccessTokenInfo();
        accessTokenInfo.transactionProcessingAccountName = 'transaction_processing';
        accessTokenInfo.riskAssessmentAccountName = 'EOS_RiskAssessment';
        config.accessTokenInfo = accessTokenInfo;

        // Set permissions specifically for client-side single-use tokenization
        config.permissions = ['PMT_POST_Create_Single'];

        // Configure service to establish connection
        ServicesContainer.configureService(config);

        // Generate session token for client-side tokenization
        const sessionToken = await GpApiService.generateTransactionKey(config);

        if (sessionToken && sessionToken.accessToken) {
            const accessToken = sessionToken.accessToken;
            console.log('Session token generated successfully: ' + accessToken.substring(0, 8) + '...');

            return res.json({
                success: true,
                data: {
                    accessToken: accessToken
                },
                message: 'Configuration retrieved successfully',
                timestamp: new Date().toISOString()
            });
        } else {
            throw new Error('Invalid session token response format');
        }
    } catch (error) {
        console.error('Configuration error: ' + error.message);
        res.status(500).json({
            success: false,
            message: 'Error loading configuration: ' + error.message,
            error_code: 'CONFIG_ERROR',
            timestamp: new Date().toISOString()
        });
    }
});

/**
 * Health check endpoint
 */
app.get('/health', async (req, res) => {
    try {
        const paymentMethods = JsonStorage.readPaymentMethods();
        const dataDir = './data';
        const fs = await import('fs');

        const response = {
            status: 'healthy',
            version: '1.0.0',
            timestamp: new Date().toISOString(),
            environment: process.env.APP_ENV || 'development',
            storage: {
                type: 'json_file',
                data_directory_exists: fs.existsSync(dataDir),
                data_directory_writable: true,
                payment_methods_count: paymentMethods.length
            },
            capabilities: {
                payment_method_creation: true,
                immediate_payments: true,
                delayed_charges: true,
                stored_payment_tokenization: !!(process.env.GP_API_APP_KEY),
                mock_fallback: true
            },
            endpoints: {
                'GET /health': 'System health check',
                'GET /config': 'Get SDK configuration',
                'GET /payment-methods': 'Get payment methods',
                'POST /payment-methods': 'Create payment method',
                'POST /charge': 'Process immediate charge ($25)',
                'GET /mock-mode': 'Get mock mode status',
                'POST /mock-mode': 'Toggle mock mode on/off'
            }
        };

        PaymentUtils.sendSuccessResponse(res, response, 'System is healthy and ready');
    } catch (error) {
        console.error('Health check error: ' + error.message);
        PaymentUtils.sendErrorResponse(res, 500, 'Health check failed', 'SERVER_ERROR');
    }
});

/**
 * Payment methods endpoint - GET all payment methods
 */
app.get('/payment-methods', (req, res) => {
    try {
        const paymentMethods = JsonStorage.readPaymentMethods();

        const formattedMethods = paymentMethods.map(method => ({
            id: method.id,
            type: 'card',
            last4: method.last4,
            brand: method.cardBrand,
            expiry: `${method.expiryMonth}/${method.expiryYear}`,
            isDefault: method.isDefault || false,
            nickname: method.nickname || ''
        }));

        PaymentUtils.sendSuccessResponse(res, formattedMethods, 'Payment methods retrieved successfully');
    } catch (error) {
        console.error('Payment methods error: ' + error.message);
        PaymentUtils.sendErrorResponse(res, 500, 'Internal server error', 'SERVER_ERROR');
    }
});

/**
 * Payment methods endpoint - POST create new or update existing payment method
 */
app.post('/payment-methods', async (req, res) => {
    try {
        const data = req.body;

        // Check if this is an edit operation (has 'id' field)
        if (data.id) {
            // Edit existing payment method
            const paymentMethodId = data.id;

            // Validate that the payment method exists
            if (!JsonStorage.paymentMethodExists(paymentMethodId)) {
                return PaymentUtils.sendErrorResponse(res, 404, 'Payment method not found', 'NOT_FOUND');
            }

            // Prepare update data (only editable fields)
            const updateData = {};
            if (data.nickname !== undefined) {
                updateData.nickname = data.nickname;
            }
            if (data.isDefault !== undefined) {
                updateData.isDefault = data.isDefault;
            }

            // Validate update data
            const validationErrors = JsonStorage.validateUpdateData(updateData);
            if (validationErrors.length > 0) {
                return PaymentUtils.sendErrorResponse(res, 400, validationErrors.join(', '), 'VALIDATION_ERROR');
            }

            // Update the payment method
            if (!JsonStorage.updatePaymentMethod(paymentMethodId, updateData)) {
                return PaymentUtils.sendErrorResponse(res, 500, 'Failed to update payment method', 'UPDATE_ERROR');
            }

            // Get updated payment method for response
            const updatedMethod = JsonStorage.findPaymentMethod(paymentMethodId);

            const response = {
                id: updatedMethod.id,
                type: 'card',
                last4: updatedMethod.last4,
                brand: updatedMethod.cardBrand,
                expiry: `${updatedMethod.expiryMonth}/${updatedMethod.expiryYear}`,
                nickname: updatedMethod.nickname || '',
                isDefault: updatedMethod.isDefault || false,
                updatedAt: updatedMethod.updatedAt
            };

            PaymentUtils.sendSuccessResponse(res, response, 'Payment method updated successfully');
        } else {
            // Create a new payment method using payment_token from GP PaymentForm
            if (!data.payment_token) {
                return PaymentUtils.sendErrorResponse(res, 400, 'Missing required payment_token', 'VALIDATION_ERROR');
            }

            if (!data.cardDetails) {
                return PaymentUtils.sendErrorResponse(res, 400, 'Missing required cardDetails', 'VALIDATION_ERROR');
            }

            const paymentMethodId = JsonStorage.generateId();
            const paymentToken = data.payment_token;
            const cardDetails = data.cardDetails;
            let mockMode = MockModeConfig.isMockModeEnabled();

            // Extract customer data from request
            const customerData = {
                first_name: data.first_name || '',
                last_name: data.last_name || '',
                email: data.email || '',
                phone: data.phone || '',
                street_address: data.street_address || '',
                city: data.city || '',
                state: data.state || '',
                billing_zip: data.billing_zip || '',
                country: data.country || ''
            };

            // Create multi-use token with customer data or use mock
            let multiUseTokenData = null;
            let finalToken = paymentToken;

            if (!mockMode && process.env.GP_API_APP_KEY) {
                try {
                    multiUseTokenData = await PaymentUtils.createMultiUseTokenWithCustomer(paymentToken, customerData, cardDetails);
                    finalToken = multiUseTokenData.multiUseToken;
                } catch (error) {
                    console.error('Multi-use token creation error: ' + error.message);

                    // Check if this is an authentication/authorization error
                    const isAuthError = error.message && (
                        error.message.includes('ACTION_NOT_AUTHORIZED') ||
                        error.message.includes('NOT_AUTHENTICATED') ||
                        error.message.includes('INVALID_CREDENTIALS') ||
                        error.message.includes('Permission')
                    );

                    if (isAuthError) {
                        return PaymentUtils.sendErrorResponse(res, 403,
                            'Authentication failed: Access token and merchant credentials do not match. Please verify your GP_API_APP_ID and GP_API_APP_KEY configuration.',
                            'AUTHENTICATION_ERROR');
                    }

                    // For other errors, return proper error response (no mock fallback)
                    return PaymentUtils.sendErrorResponse(res, 500,
                        'Failed to create payment method: ' + error.message,
                        'TOKEN_CREATION_ERROR');
                }
            }

            // Use mock data in mock mode or if token creation failed
            if (mockMode || !multiUseTokenData) {
                const brand = PaymentUtils.determineCardBrandFromType(cardDetails.cardType || '');
                multiUseTokenData = {
                    multiUseToken: paymentToken,
                    brand: brand,
                    last4: cardDetails.cardLast4 || '',
                    expiryMonth: cardDetails.expiryMonth || '',
                    expiryYear: cardDetails.expiryYear || '',
                    customerData: customerData
                };
            }

            const validationData = {
                cardBrand: multiUseTokenData.brand,
                last4: multiUseTokenData.last4,
                expiryMonth: multiUseTokenData.expiryMonth,
                expiryYear: multiUseTokenData.expiryYear
            };

            const validationErrors = JsonStorage.validatePaymentMethod(validationData);
            if (validationErrors.length > 0) {
                return PaymentUtils.sendErrorResponse(res, 400, validationErrors.join(', '), 'VALIDATION_ERROR');
            }

            const paymentMethod = {
                id: paymentMethodId,
                storedPaymentToken: finalToken,
                cardBrand: multiUseTokenData.brand,
                last4: multiUseTokenData.last4,
                expiryMonth: multiUseTokenData.expiryMonth,
                expiryYear: multiUseTokenData.expiryYear,
                nickname: data.nickname || `${multiUseTokenData.brand} ending in ${multiUseTokenData.last4}`,
                isDefault: data.isDefault || false,
                customerData: customerData
            };

            if (!JsonStorage.addPaymentMethod(paymentMethod)) {
                return PaymentUtils.sendErrorResponse(res, 500, 'Failed to save payment method', 'STORAGE_ERROR');
            }

            const response = {
                id: paymentMethodId,
                storedPaymentToken: finalToken,
                type: 'card',
                last4: multiUseTokenData.last4,
                brand: multiUseTokenData.brand,
                expiry: `${multiUseTokenData.expiryMonth}/${multiUseTokenData.expiryYear}`,
                nickname: paymentMethod.nickname,
                isDefault: paymentMethod.isDefault,
                mockMode: mockMode
            };

            PaymentUtils.sendSuccessResponse(res, response, 'Payment method created and saved successfully');
        }
    } catch (error) {
        console.error('Payment methods error: ' + error.message);
        PaymentUtils.sendErrorResponse(res, 500, 'Internal server error', 'SERVER_ERROR');
    }
});

/**
 * Mock mode endpoint - GET status
 */
app.get('/mock-mode', (req, res) => {
    try {
        const isEnabled = MockModeConfig.isMockModeEnabled();

        const mockModeConfig = {
            isEnabled: isEnabled
        };

        PaymentUtils.sendSuccessResponse(res, mockModeConfig, `Mock mode is ${MockModeConfig.getMockModeText()}`);
    } catch (error) {
        console.error('Mock mode error: ' + error.message);
        PaymentUtils.sendErrorResponse(res, 500, 'Internal server error', 'SERVER_ERROR');
    }
});

/**
 * Mock mode endpoint - POST toggle
 */
app.post('/mock-mode', (req, res) => {
    try {
        const data = req.body;

        if (data.isEnabled === undefined || typeof data.isEnabled !== 'boolean') {
            return PaymentUtils.sendErrorResponse(res, 400, 'Invalid JSON format: isEnabled field is required and must be boolean', 'VALIDATION_ERROR');
        }

        const isEnabled = data.isEnabled;

        if (!MockModeConfig.setMockModeEnabled(isEnabled)) {
            return PaymentUtils.sendErrorResponse(res, 500, 'Failed to update mock mode configuration', 'CONFIG_ERROR');
        }

        const mockModeConfig = {
            isEnabled: isEnabled
        };

        PaymentUtils.sendSuccessResponse(res, mockModeConfig, `Mock mode ${MockModeConfig.getMockModeText()} successfully`);
    } catch (error) {
        console.error('Mock mode error: ' + error.message);
        PaymentUtils.sendErrorResponse(res, 500, 'Internal server error', 'SERVER_ERROR');
    }
});

// Start the server
app.listen(port, '0.0.0.0', () => {
    console.log(`Server running at http://localhost:${port}`);
    console.log('Wallet Management System with GP API');
});
