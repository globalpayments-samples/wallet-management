# .NET Wallet Management System

This example demonstrates a comprehensive wallet management system using ASP.NET Core and the Global Payments SDK. It includes payment method management, secure tokenization, mock testing capabilities, and a complete web interface.

## Features

- **Payment Method Management** - Store, retrieve, and manage customer payment methods securely
- **Tokenization** - Securely tokenize and store payment methods using Global Payments vault
- **Multi-Use Token Creation** - Convert single-use tokens to multi-use stored payment tokens with customer data
- **Wallet Management** - Secure storage and management of payment methods
- **Mock Mode** - Test wallet flows with simulated responses without hitting live APIs
- **Comprehensive UI** - Complete web interface with payment method management
- **Test Card Integration** - Built-in Heartland certification test cards for development and testing

## Requirements

- .NET 9.0 or later
- Global Payments account and API credentials

## Project Structure

- `Program.cs` - Main application with all payment processing logic and API endpoints
- `Models.cs` - Data models and response structures
- `PaymentUtils.cs` - Payment utility functions and SDK integration
- `JsonStorage.cs` - JSON-based storage for payment methods
- `MockResponses.cs` - Mock data generation for testing scenarios
- `wwwroot/index.html` - Complete web interface with payment management
- `.env.sample` - Template for environment variables
- `run.sh` - Convenience script to run the application

## Recent Improvements

### ✅ Expiration Date Handling Fix (September 2024)
Fixed a critical issue where payment methods with 4-digit expiration years (e.g., "2028") were causing API errors. The system now correctly:
- Accepts both 2-digit ("28") and 4-digit ("2028") year formats from the frontend
- Converts 4-digit years to 2-digit format for SDK compatibility  
- Maintains backward compatibility with existing implementations
- Works seamlessly with all built-in test cards

## Setup

1. Clone this repository
2. Copy `.env.sample` to `.env`
3. Update `.env` with your Global Payments credentials:
   ```
   GP_API_APP_ID=your_app_id
   GP_API_APP_KEY=your_app_key
   GP_API_ENVIRONMENT=sandbox
   ```
4. Install dependencies:
   ```bash
   dotnet restore
   ```
5. Run the application:
   ```bash
   ./run.sh
   ```
   Or manually:
   ```bash
   dotnet run
   ```
6. Open your browser to `http://localhost:8000`

## API Endpoints

### GET /health
System health check endpoint.

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "healthy",
    "timestamp": "2024-09-08T14:00:00Z",
    "service": "vault-one-click-dotnet",
    "version": "1.0.0"
  },
  "message": "System is healthy"
}
```

### GET /config
Returns configuration for frontend SDK initialization.

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "generated_session_token"
  },
  "message": "Configuration retrieved successfully"
}
```

### GET /payment-methods
Retrieve all stored payment methods for the customer.

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "pm_123456789",
      "type": "card",
      "last4": "1234",
      "brand": "Visa",
      "expiry": "12/28",
      "isDefault": true,
      "nickname": "My Primary Card"
    }
  ]
}
```

### POST /payment-methods
Create multi-use token with customer data or edit an existing payment method.

**Create Multi-Use Token Request:**
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

**Legacy Card Entry Request:**
```json
{
  "cardNumber": "4012002000060016",
  "expiryMonth": "12",
  "expiryYear": "2028",
  "cvv": "123",
  "nickname": "Test Visa Card",
  "isDefault": true
}
```

**Edit Request:**
```json
{
  "id": "pm_123456789",
  "nickname": "Updated Nickname",
  "isDefault": false
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "pm_123456789",
    "storedPaymentToken": "vault_abc123def456",
    "type": "card",
    "last4": "0016",
    "brand": "Visa",
    "expiry": "12/28",
    "nickname": "Test Visa Card",
    "isDefault": true,
    "mockMode": false
  }
}
```

### GET /mock-mode
Get current mock mode status.

**Response:**
```json
{
  "success": true,
  "data": {
    "isEnabled": false
  },
  "message": "Mock mode is disabled"
}
```

### POST /mock-mode
Toggle mock mode on/off.

**Request:**
```json
{
  "isEnabled": true
}
```

## Mock Mode

Mock mode allows you to test payment flows without hitting live APIs:
- **Enable/Disable**: Use the toggle in the web interface or the `/mock-mode` endpoint
- **Simulated Responses**: Generates realistic transaction data
- **Test Scenarios**: Different card numbers produce different response scenarios
- **Safe Testing**: No actual charges or API calls are made
- **Development**: Perfect for development and integration testing

## Built-in Test Cards

The system includes Heartland certification test cards:
- **Visa**: 4012002000060016
- **MasterCard**: 2223000010005780, 5473500000000014
- **Discover**: 6011000990156527  
- **American Express**: 372700699251018
- **JCB**: 3566007770007321

All test cards use:
- **Expiry**: 12/2028 (automatically handled by the expiration date fix)
- **CVV**: 123 (1234 for Amex)

## Implementation Details

### SDK Configuration
- Uses GpApiConfig for Global Payments GP API setup
- Loads credentials from environment variables (GP_API_APP_ID, GP_API_APP_KEY)
- Configures environment (sandbox/production) via GP_API_ENVIRONMENT
- Channel set to CardNotPresent for online transactions

### Wallet Management
1. **Multi-Use Tokenization**: Convert single-use tokens to multi-use stored payment tokens with customer data
2. **Customer Integration**: Associate customer billing information with payment methods
3. **Storage**: Store enhanced payment method metadata with customer context in JSON format
4. **Management**: Secure storage and management of payment methods ready for future payment processing
5. **Error Handling**: Comprehensive error handling with meaningful messages and type safety

### Data Storage
- JSON file-based storage for payment methods
- Thread-safe operations for concurrent access
- Automatic backup and recovery capabilities
- Easy migration to database systems

### Security Features
- Tokenization ensures sensitive data never touches your servers
- Environment-based configuration management
- CORS protection for API endpoints
- Input validation and sanitization

## Multi-Use Token Implementation

The .NET implementation converts single-use tokens to multi-use stored payment tokens using GP API's charge-based approach:

### Key Features

- **Charge-Based Tokenization**: Uses a minimal 0.01 GBP charge to convert single-use tokens to multi-use
- **Customer Data Integration**: Associates customer billing information with payment methods
- **CAPTURED Validation**: Validates response with "CAPTURED" status from GP API
- **Type-Safe Implementation**: Uses strongly-typed C# models for all token operations

### Token Creation Process

```csharp
public static async Task<MultiUseTokenResult> CreateMultiUseTokenWithCustomerAsync(
    string paymentToken,
    CustomerData customerData,
    CardDetails cardDetails)
{
    var card = new CreditCardData
    {
        Token = paymentToken,
        CardHolderName = $"{customerData.FirstName} {customerData.LastName}"
    };

    var address = new Address
    {
        StreetAddress1 = customerData.StreetAddress,
        City = customerData.City,
        Province = customerData.State,
        PostalCode = SanitizePostalCode(customerData.BillingZip),
        Country = customerData.Country
    };

    // Charge 0.01 GBP to convert single-use to multi-use token
    var response = card.Charge(0.01m)
        .WithCurrency("GBP")
        .WithRequestMultiUseToken(true)
        .WithAddress(address)
        .Execute();

    // Validate GP API response
    if (response.ResponseCode == "SUCCESS" &&
        response.ResponseMessage == "CAPTURED")
    {
        return new MultiUseTokenResult
        {
            MultiUseToken = response.Token ?? paymentToken,
            Brand = DetermineCardBrandFromType(cardDetails.CardType),
            Last4 = cardDetails.CardLast4,
            ExpiryMonth = cardDetails.ExpiryMonth,
            ExpiryYear = cardDetails.ExpiryYear,
            CustomerData = customerData
        };
    }

    throw new Exception("Multi-use token creation failed");
}
```

### Implementation Benefits

- **GP API Native**: Uses GP API's recommended charge-based approach
- **Type Safety**: Strongly-typed models prevent runtime errors
- **Customer Context**: Enhanced user experience with stored customer data
- **PCI Compliance**: Card data never touches your servers
- **Validation**: Explicit CAPTURED status check ensures successful tokenization

## Production Considerations

For production deployment, enhance with:
- **Database Integration** - Replace JSON storage with Entity Framework and SQL Server
- **Authentication** - Add ASP.NET Core Identity for user authentication
- **Rate Limiting** - Implement API rate limiting middleware
- **Monitoring** - Add Application Insights and structured logging
- **Security** - Implement additional security headers and validation
- **PCI Compliance** - Follow PCI DSS requirements for payment processing
- **Error Handling** - Enhanced error handling with proper exception filters
- **Testing** - Comprehensive unit and integration tests with xUnit

## Troubleshooting

### Common Issues

**.NET Version Issues**:
- Ensure .NET 6.0 or later is installed
- Check version with `dotnet --version`
- Verify SDK is installed: `dotnet --list-sdks`
- Update .NET from https://dotnet.microsoft.com/download

**NuGet Package Issues**:
- Restore packages: `dotnet restore`
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Check for package conflicts in project file
- Verify package sources: `dotnet nuget list source`

**Build Configuration Issues**:
- Check for compilation errors: `dotnet build`
- Verify project file (.csproj) configuration
- Ensure proper target framework is specified
- Check for missing references or using statements

**Port and Hosting Issues**:
- Default port conflicts (check if 5000/5001 are available)
- Configure custom port: `dotnet run --urls "http://localhost:8080"`
- Verify HTTPS certificate for development
- Check firewall settings for port access

**Expiration Date Errors (Fixed)**:
- Issue: 4-digit years causing API errors
- Solution: Automatic conversion implemented
- Status: ✅ Resolved in recent update

**API Configuration**:
- Ensure GP_API_APP_ID and GP_API_APP_KEY are set in .env file
- Verify credentials are for the correct environment (sandbox/production)
- Set GP_API_ENVIRONMENT to "sandbox" or "production"
- Validate .env file loading on startup

**Payment Processing**:
- Use test cards in certification environment
- Enable mock mode for development testing
- Check console logs for detailed error information
- Verify proper exception handling in payment endpoints

**File System Permissions**:
- Ensure write permissions for data storage directory
- Check that JSON storage files can be created and modified
- Verify proper file paths in storage operations
- Set appropriate directory permissions on deployment

**CORS Issues**:
- Check browser developer console for CORS errors
- Verify CORS policy configuration in Program.cs
- Ensure proper preflight request handling
- Check allowed origins, methods, and headers

**ASP.NET Core Issues**:
- Verify middleware registration order
- Check for dependency injection configuration errors
- Ensure proper controller routing
- Validate model binding and validation attributes