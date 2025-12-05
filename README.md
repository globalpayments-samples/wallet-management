# Global Payments Wallet Management System

A comprehensive wallet management system for storing and managing payment methods using Global Payments multi-use tokens. This system provides secure payment method storage across multiple programming languages, demonstrating how to tokenize and store customer payment information without processing actual payments.

## Available Implementations

- [.NET Core](./dotnet/) - ([Preview](https://githubbox.com/globalpayments-samples/wallet-management/tree/main/dotnet)) - ASP.NET Core web application
- [Java](./java/) - ([Preview](https://githubbox.com/globalpayments-samples/wallet-management/tree/main/java)) - Jakarta EE servlet-based web application
- [Node.js](./nodejs/) - ([Preview](https://githubbox.com/globalpayments-samples/wallet-management/tree/main/nodejs)) - Express.js web application
- [PHP](./php/) - ([Preview](https://githubbox.com/globalpayments-samples/wallet-management/tree/main/php)) - PHP web application

## System Features

- **Multi-Use Token Creation** - Convert single-use tokens to secure stored payment tokens with customer data
- **Payment Method Management** - Add, view, edit, and manage stored payment methods
- **Mock Mode Testing** - Test functionality without live API credentials
- **JSON-Based Storage** - Simple file-based storage for demonstration purposes
- **Customer Data Integration** - Associate billing information with payment methods
- **Multiple Languages** - Consistent API across all implementations

## Core Functionality

Each implementation provides:

1. **Wallet Management**
   - Create and store payment methods with multi-use tokens
   - List all saved payment methods
   - Edit payment method details (nickname, default status)
   - Associate customer billing information

2. **API Endpoints**
   - GET `/health` - System health check
   - GET `/config` - Get public API configuration
   - GET `/payment-methods` - List stored payment methods
   - POST `/payment-methods` - Create or edit payment method
   - GET `/mock-mode` - Check mock mode status
   - POST `/mock-mode` - Toggle mock mode

3. **Security Features**
   - Tokenized payment storage (no raw card data)
   - Environment-based credential management
   - PCI-compliant architecture

## Quick Start

1. **Choose your implementation** - Navigate to any implementation directory (nodejs, php, java, dotnet)
2. **Set up credentials** - Copy `.env.sample` to `.env` and add your Global Payments API keys
3. **Run the server** - Execute `./run.sh` or follow implementation-specific instructions
4. **Access the interface** - Open `http://localhost:8000` (or implementation-specific port)
5. **Test with mock mode** - Enable mock mode to test without live credentials

## How It Works

The wallet management system demonstrates:

- **Token Creation** - Convert single-use tokens (from frontend) to multi-use tokens (stored in backend)
- **Customer Association** - Link payment methods with customer billing information
- **Secure Storage** - Store tokens with associated metadata (last4, brand, expiry)
- **Payment Method Retrieval** - List and manage stored payment methods
- **Mock Testing** - Test all functionality without live API credentials

## Prerequisites

- Global Payments account with API credentials (optional for mock mode)
- Development environment for your chosen language
- Package manager (npm, composer, maven, or dotnet)

## Implementation Details

Each implementation follows the same architecture:

1. **Frontend** - HTML/JavaScript with Global Payments tokenization
2. **Backend** - REST API handling token conversion and storage
3. **Storage** - JSON file-based storage (replace with database for production)
4. **SDK Integration** - Global Payments SDK for multi-use token creation

### Technology Stack by Implementation

- **PHP** - Pure PHP with Global Payments SDK, JSON storage
- **Java** - Jakarta EE servlets with Global Payments SDK
- **.NET** - ASP.NET Core Minimal API with Global Payments SDK
- **Node.js** - Express.js with Global Payments SDK

## Production Considerations

For production deployment, enhance the system with:

- **Database Storage** - Replace JSON with PostgreSQL/MySQL/MongoDB
- **Authentication** - Add user authentication and authorization
- **Security** - Implement HTTPS, rate limiting, and security headers
- **PCI Compliance** - Ensure proper handling of payment data
- **Monitoring** - Add logging, alerting, and performance monitoring
- **Scalability** - Implement caching and load balancing
