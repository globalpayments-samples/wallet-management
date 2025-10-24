# Wallet Management System

A PHP-based wallet management system that creates multi-use tokens with customer data using the Global Payments GP API. Features secure payment method storage with integrated customer information for wallet management.

## Requirements

- PHP 7.4 or later
- Composer
- Global Payments GP API account and credentials (optional - includes mock mode)

## Key Features

- **Multi-Use Tokens with Customer Data**: Creates stored payment tokens that include customer billing information
- **Integrated Customer Management**: Associates customer details directly with payment tokens
- **Wallet Management**: Secure storage and management of payment methods
- **Mock Mode Support**: Test functionality without live API credentials
- **Customer Data Storage**: Associates customer billing information with stored payment tokens

## Project Structure

- `index.html` - Complete user interface with add card and payment methods management
- `payment-methods.php` - Multi-use token creation with customer data integration
- `PaymentUtils.php` - Core SDK utilities including customer token creation
- `JsonStorage.php` - Payment method and customer data storage
- `config.php` - Frontend SDK configuration
- `health.php` - System status monitoring

## Setup

1. Clone this repository
2. Copy `.env.sample` to `.env` (optional)
3. Update `.env` with your Global Payments GP API credentials (if using real processing):
   ```
   APP_ID=your_gp_api_app_id
   APP_KEY=your_gp_api_app_key
   GP_API_ENVIRONMENT=sandbox
   ```
   Get your credentials from: https://developer.globalpay.com/
4. Install dependencies:
   ```bash
   composer install
   ```
5. Run the application:
   ```bash
   ./run.sh
   ```
   Or manually:
   ```bash
   php -S localhost:8000
   ```

## How It Works

### 1. Customer Card Entry
Users enter their billing information and payment details through a secure Global Payments tokenization form. The frontend collects:
- Customer details (name, email, phone, billing address)
- Payment card information (handled by Global Payments JS SDK)

### 2. Multi-Use Token Creation (GP API)
The system creates enhanced stored payment tokens with integrated customer data using GP API's charge-based flow:
- Single-use payment tokens are converted to multi-use stored payment tokens via a verification charge
- GP API requires a minimal charge ($0.01) to create multi-use tokens (PMT_xxx format)
- Customer billing information is associated with each payment method
- Address and contact data enables enhanced fraud protection and user experience
- Customer context is maintained for regulatory compliance and support

### 3. Wallet Management
Saved payment methods enable secure storage and management:
- View and manage saved payment methods with customer context
- Stored payment tokens ready for future payment processing integration
- Full customer and payment method details

### 4. Data Flow
```
Frontend Form → GP API Tokenization (Single-Use) → Backend Charge ($0.01) → Multi-Use Token (PMT_xxx) + Customer Data → Secure Storage → Wallet Management
```

### Key Implementation (GP API)
- **Frontend**: Uses GP API access token for secure client-side single-use tokenization
- **Backend**: Converts single-use to multi-use tokens via charge with `createMultiUseTokenWithCustomer()` method
- **Token Format**: Multi-use tokens use PMT_xxx prefix (GP API standard)
- **Storage**: JSON-based persistence including customer information with payment methods
- **Processing**: Uses stored stored payment tokens with associated customer data for payments

## API Endpoints

### GET /health
System health check with detailed status information.

Response:
```json
{
  "success": true,
  "message": "System is healthy",
  "data": {
    "status": "healthy",
    "timestamp": "2024-01-01T12:00:00Z",
    "sdk_status": "available",
    "storage_status": "operational"
  }
}
```

### GET /payment-methods
Retrieve all saved payment methods.

Response:
```json
{
  "success": true,
  "data": [
    {
      "id": "pm_123",
      "type": "card",
      "last4": "4242",
      "brand": "visa",
      "expiry": "12/25",
      "isDefault": true,
      "nickname": "My Visa Card"
    }
  ]
}
```

### POST /payment-methods
Create multi-use token with customer data.

Request:
```json
{
  "payment_token": "supt_abc123",
  "cardDetails": {
    "cardType": "visa",
    "cardLast4": "4242",
    "expiryMonth": "12",
    "expiryYear": "2028"
  },
  "first_name": "Jane",
  "last_name": "Doe",
  "email": "jane@example.com",
  "phone": "5551234567",
  "street_address": "123 Main St",
  "city": "Anytown",
  "state": "NY",
  "billing_zip": "12345",
  "country": "USA",
  "nickname": "My Visa Card",
  "isDefault": true
}
```

Response:
```json
{
  "success": true,
  "data": {
    "id": "pm_123",
    "storedPaymentToken": "multi_use_token_xyz",
    "type": "card",
    "last4": "4242",
    "brand": "Visa",
    "expiry": "12/28",
    "nickname": "My Visa Card",
    "isDefault": true,
    "mockMode": false
  }
}
```

## Multi-Use Token Implementation (GP API)

This system creates multi-use tokens using GP API's charge-based approach, combining stored payment tokens with customer data for secure wallet management.

### Key Features

- **GP API Charge-Based Flow**: Converts single-use tokens to multi-use via verification charge
- **Customer Data Integration**: Associates customer billing information with payment methods
- **Secure Storage**: Maintains PCI compliance while storing customer context
- **Wallet Management**: Enables secure storage of payment methods using stored PMT_xxx tokens

### Token Creation Process (GP API)

1. **Frontend Collection**: Customer enters payment and billing details
2. **GP API Tokenization**: Card data is tokenized by GP API JS SDK (single-use token)
3. **Backend Verification Charge**: $0.01 charge converts single-use to multi-use token
4. **Multi-Use Token Response**: GP API returns PMT_xxx format multi-use token
5. **Secure Storage**: Enhanced token with customer context is stored securely

### Customer Data Integration

The system enhances stored payment tokens with comprehensive customer information using GP API:

```php
// Example of GP API multi-use token creation with customer data
$multiUseToken = PaymentUtils::createMultiUseTokenWithCustomer(
    $singleUseToken,  // From frontend GP API tokenization
    [
        'first_name' => 'Jane',
        'last_name' => 'Doe',
        'email' => 'jane@example.com',
        'phone' => '5551234567',
        'street_address' => '123 Main St',
        'city' => 'Anytown',
        'state' => 'NY',
        'billing_zip' => '12345',
        'country' => 'USA'
    ],
    $cardDetails  // Card metadata from frontend
);

// GP API performs $0.01 charge and returns multi-use token (PMT_xxx)
```

### Implementation Benefits

- **Reduced PCI Scope**: Card data never touches your servers
- **Enhanced UX**: Customers see full context (name, address) with saved cards
- **Fraud Reduction**: Consistent customer data validation
- **Regulatory Compliance**: Maintains data protection standards

### Token Lifecycle

1. **Creation**: Single-use token + customer data → Multi-use vault token
2. **Storage**: Token metadata with customer context stored locally
3. **Usage**: Multi-use token enables repeat payments without re-entry
4. **Management**: Update customer data or payment preferences

### Advanced Use Cases

Multi-use tokens enable various integration scenarios:

- **Payment Processing Integration**: Ready for payment processing system integration
- **Customer Portal**: Manage saved payment methods with customer context
- **Billing Management**: Update billing addresses without new tokenization
- **Enhanced UX**: Pre-filled customer data for future payment flows

## Troubleshooting

### Common Issues

**Setup Issues**:
- Ensure PHP 7.4+ is installed: `php --version`
- Run `composer install` to install dependencies
- Set up `.env` file with Global Payments credentials (optional for mock mode)

**Payment Processing**:
- Use GP API test cards: 4263970000005262 (Visa), 5425230000004415 (Mastercard)
- Test credentials available from: https://developer.globalpay.com/
- Enable mock mode toggle in UI for testing without credentials
- Check browser console for frontend errors
- Verify API responses in Network tab

**Configuration**:
- Ensure `data/` directory has write permissions
- Verify CORS headers are properly set
- Check that GP API SDK loads correctly
- Verify GP API access token generation in config.php

### Mock Mode Testing
The application includes comprehensive mock mode for testing without live API credentials:
- Toggle mock mode in the UI header
- Test various card scenarios using different test card numbers
- All functionality works identically in mock mode

## Security Considerations

For production use:
- Use HTTPS for all communications
- Implement proper input validation and sanitization
- Add rate limiting and fraud detection
- Enable comprehensive logging and monitoring
- Use environment variables for sensitive configuration
- Implement CSRF protection for forms
- Regular security updates for dependencies
