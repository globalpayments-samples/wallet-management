# Java Wallet Management System

This example demonstrates a comprehensive wallet management system using Jakarta EE and the Global Payments SDK. It includes payment method management, secure tokenization, mock testing capabilities, and a complete web interface.

## Features

- **Payment Method Management** - Store, retrieve, and manage customer payment methods securely
- **Tokenization** - Securely tokenize and store payment methods using Global Payments wallet
- **Multi-Use Token Creation** - Convert single-use tokens to multi-use stored payment tokens with customer data
- **Wallet Management** - Secure storage and management of payment methods
- **Mock Mode** - Test wallet flows with simulated responses without hitting live APIs
- **Comprehensive UI** - Complete web interface with payment method management
- **Test Card Integration** - Built-in Heartland certification test cards for development and testing

## Requirements

- Java 11 or later
- Maven 3.6 or later
- Global Payments account and API credentials

## Project Structure

- `src/main/java/com/globalpayments/example/`:
  - `HealthServlet.java` - System health check endpoint
  - `PaymentMethodsServlet.java` - Payment method CRUD operations
  - `MockModeServlet.java` - Mock mode toggle functionality
  - `PaymentUtils.java` - Payment utility functions and SDK integration
  - `JsonStorage.java` - JSON-based storage for payment methods
  - `MockResponses.java` - Mock data generation for testing scenarios
- `src/main/webapp/index.html` - Complete web interface with payment management
- `pom.xml` - Maven dependencies and build configuration with Tomcat plugin
- `.env.sample` - Template for environment variables
- `run.sh` - Convenience script to run the application

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
   mvn clean install
   ```
5. Run the application:
   ```bash
   ./run.sh
   ```
   Or manually:
   ```bash
   mvn cargo:run
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
    "timestamp": "2024-09-08T14:00:00",
    "service": "vault-one-click-java",
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
      "expiry": "12/2028",
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
    "expiry": "12/2028",
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
- **Simulated Responses**: Generates realistic transaction data with proper field formatting
- **Test Scenarios**: Different card numbers produce different response scenarios (success, declines, errors)
- **Safe Testing**: No actual charges or API calls are made
- **Development**: Perfect for development and integration testing

### Mock Response Scenarios
- **Success Cards**: Last 4 digits 1111, 4242, 0000
- **Decline Cards**: Last 4 digits 0002 (insufficient funds), 0004 (generic decline), 0051 (expired)
- **Error Cards**: Last 4 digits 0091 (processing error), 0096 (system error)

## Built-in Test Cards

The system includes Heartland certification test cards:
- **Visa**: 4012002000060016
- **MasterCard**: 2223000010005780, 5473500000000014
- **Discover**: 6011000990156527  
- **American Express**: 372700699251018
- **JCB**: 3566007770007321

All test cards use:
- **Expiry**: 12/2028
- **CVV**: 123 (1234 for Amex)

## Implementation Details

### Servlet Architecture
- **Jakarta EE**: Modern servlet-based architecture using Jakarta EE 9+
- **Maven Cargo**: Embedded Tomcat server for easy development and deployment
- **Modular Design**: Separate servlets for different API endpoints
- **Thread Safety**: Concurrent request handling with thread-safe storage

### SDK Configuration
- Uses GpApiConfig for Global Payments GP API setup
- Loads credentials from .env file using dotenv-java library (GP_API_APP_ID, GP_API_APP_KEY)
- Configures environment (sandbox/production) via GP_API_ENVIRONMENT
- Channel set to CardNotPresent for online transactions

### Wallet Management
1. **Multi-Use Tokenization**: Convert single-use tokens to multi-use stored payment tokens with customer data using SDK
2. **Customer Integration**: Associate customer billing information with payment methods for enhanced context
3. **Storage**: Store enhanced payment method metadata with customer context in JSON format with thread-safe operations
4. **Management**: Secure storage and management of payment methods ready for future payment processing
5. **Error Handling**: Comprehensive error handling with meaningful HTTP status codes

### Data Storage
- JSON file-based storage for payment methods using JsonStorage utility
- Thread-safe operations for concurrent servlet access
- Automatic file locking and recovery capabilities
- Easy migration path to database systems

### Field Naming Consistency
- **Live Mode**: All response fields use camelCase formatting (transactionId, storedPaymentToken, etc.)
- **Mock Mode**: Consistent camelCase field naming across all responses including multi-use token fields
- **Frontend Compatibility**: Ensures seamless integration with JavaScript frontend
- **API Standards**: Follows modern REST API naming conventions for enhanced developer experience

## Multi-Use Token Implementation

The Java implementation converts single-use tokens to multi-use stored payment tokens using GP API's charge-based approach:

### Key Features

- **Charge-Based Tokenization**: Uses a minimal $0.01 USD charge to convert single-use tokens to multi-use
- **Customer Data Integration**: Associates customer billing information with payment methods
- **CAPTURED Validation**: Validates response with "CAPTURED" status from GP API
- **Thread-Safe Processing**: Concurrent servlet handling for multi-use token creation
- **Type-Safe Implementation**: Uses strongly-typed Java classes for all token operations

### Token Creation Process

```java
public static MultiUseTokenResult createMultiUseTokenWithCustomer(
        String paymentToken, CustomerData customerData, CardDetails cardDetails) throws Exception {

    CreditCardData card = new CreditCardData();
    card.setToken(paymentToken);
    card.setCardHolderName((customerData.firstName + " " + customerData.lastName).trim());

    Address address = new Address();
    address.setStreetAddress1(customerData.streetAddress.trim());
    address.setCity(customerData.city.trim());
    address.setState(customerData.state.trim());
    address.setPostalCode(sanitizePostalCode(customerData.billingZip));
    address.setCountry(customerData.country.trim());

    // Charge $0.01 USD to convert single-use to multi-use token
    Transaction response = card.charge(new BigDecimal("0.01"))
            .withCurrency("USD")
            .withRequestMultiUseToken(true)
            .withAddress(address)
            .execute();

    // Validate GP API response
    if ("SUCCESS".equals(response.getResponseCode()) &&
        "CAPTURED".equals(response.getResponseMessage())) {

        return new MultiUseTokenResult(
            response.getToken() != null ? response.getToken() : paymentToken,
            determineCardBrandFromType(cardDetails.cardType),
            cardDetails.cardLast4,
            cardDetails.expiryMonth,
            cardDetails.expiryYear,
            customerData
        );
    }

    throw new Exception("Multi-use token creation failed");
}
```

### Implementation Benefits

- **GP API Native**: Uses GP API's recommended charge-based approach
- **Thread Safety**: Servlet-based architecture with concurrent request handling
- **Type Safety**: Compile-time validation with Java's type system
- **Enterprise Ready**: Built on Jakarta EE standards for scalability
- **Memory Management**: Efficient JVM memory management for customer data
- **Validation**: Explicit CAPTURED status check ensures successful tokenization

## Production Considerations

For production deployment, enhance with:
- **Database Integration** - Replace JSON storage with JPA/Hibernate and PostgreSQL/MySQL
- **Authentication** - Add Spring Security or Jakarta Security for robust authentication
- **Connection Pooling** - Configure HikariCP or c3p0 for database connection pooling
- **Caching** - Implement Redis or Hazelcast for frequently accessed customer data
- **Rate Limiting** - Implement servlet filters for API rate limiting
- **Monitoring** - Add SLF4J with Logback and APM tools like New Relic or AppDynamics
- **Security** - Implement additional security filters and OWASP compliance
- **Container Deployment** - Deploy to production servlet containers (Tomcat, Jetty, WildFly)
- **PCI Compliance** - Follow PCI DSS requirements for payment processing
- **Testing** - Comprehensive unit and integration tests with JUnit 5 and TestContainers

## Build and Deployment

### Development
```bash
mvn clean compile
mvn cargo:run
```

### Production Build
```bash
mvn clean package
# Deploys ROOT.war to target/
```

### Docker Support
```bash
# Build with Maven
mvn clean package
# Deploy war file to your servlet container
```

## Troubleshooting

### Common Issues

**Java Version Issues**:
- Ensure Java 11 or later is installed
- Check version with `java --version`
- Verify JAVA_HOME environment variable is set correctly
- Update Java from https://adoptium.net/ or your distribution package manager

**Maven Build Issues**:
- Ensure Java 11+ is installed and JAVA_HOME is set
- Run `mvn clean install` to resolve dependency issues
- Check internet connection for Maven Central downloads
- Clear Maven local repository: `mvn dependency:purge-local-repository`
- Verify Maven settings.xml for proxy or mirror configurations

**Servlet Container Issues**:
- Verify Tomcat embedded server starts correctly
- Check for conflicting servlet dependencies
- Ensure proper web.xml configuration (if using traditional deployment)
- Verify context path and servlet mappings

**Port Conflicts**:
- Default port is 8000, modify `pom.xml` if needed:
  ```xml
  <cargo.servlet.port>8080</cargo.servlet.port>
  ```
- Check if port is already in use: `netstat -an | grep 8000`
- Verify firewall settings allow access to configured port

**API Configuration**:
- Ensure GP_API_APP_ID and GP_API_APP_KEY are set in .env file
- Verify credentials are for the correct environment (sandbox/production)
- Set GP_API_ENVIRONMENT to "sandbox" or "production"
- Check servlet initialization logs for SDK configuration errors
- Validate environment variable loading in servlet context

**Payment Processing**:
- Use test cards in certification environment
- Enable mock mode for development testing
- Check server logs for detailed error information and stack traces
- Verify proper exception handling in servlet methods

**File System Permissions**:
- Ensure write permissions for data storage directory
- Check that JSON storage files can be created and modified
- Verify proper file paths in storage operations
- Set appropriate directory permissions on deployment server

**CORS Issues**:
- Check browser developer console for CORS errors
- Verify CORS filter configuration in web.xml or servlet annotations
- Ensure proper preflight request handling
- Validate allowed origins, methods, and headers in CORS configuration

**Jakarta EE/Servlet Issues**:
- Verify proper servlet API version (Jakarta EE 9+ uses jakarta.servlet.*)
- Check for servlet lifecycle issues in initialization
- Ensure proper request/response handling in doGet/doPost methods
- Validate servlet mapping annotations or web.xml configuration