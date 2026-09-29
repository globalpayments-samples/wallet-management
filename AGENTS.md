# Global Payments Wallet Management

> Save cards to a customer wallet by converting a Drop-In UI single-use token into a GP-API multi-use token, stored in a local JSON file, demonstrated in PHP, Node.js, Java, and .NET.

## Critical Patterns

1. **Multi-use tokens are created with a real $0.01 USD charge, not a verify.** Every implementation calls `charge(0.01).withCurrency("USD").withRequestMultiUseToken(true).withAddress(address)` on a `CreditCardData` whose `token` is the single-use `PMT_...` reference, and only accepts the result when `responseCode` is `SUCCESS` and `responseMessage` is `CAPTURED`. The stored token is `response.token`. The code comments state GP-API requires a charge here, so every card saved outside mock mode moves one cent. Keep this in mind before pointing the sample at production credentials.

2. **Two separate GP-API configs: a restricted one for the browser and a full one for the server.** `GET /config` builds a `GpApiConfig` with `permissions = ["PMT_POST_Create_Single"]` and returns only the access token from `GpApiService.generateTransactionKey()`; the browser feeds it to `GlobalPayments.configure({ accessToken, env: "sandbox", apiVersion: "2021-03-22" })`. The server-side config from `configureSdk()` has no permission restriction and is what the $0.01 charge runs on. In Node.js the `/config` handler also calls `ServicesContainer.configureService()` with the restricted config, which replaces the startup config in the running process. PHP does the same, but each PHP request is a fresh process so it does not carry over.

3. **Mock mode silently replaces the gateway.** When mock mode is on, the single-use token is stored as if it were the multi-use token and brand, last4, and expiry come from the browser's `cardDetails` (Java uses `MockResponses.getCardDetailsFromToken()` instead). Node.js, PHP, and .NET also fall back to mock data when `GP_API_APP_KEY` is empty; Java returns `503 CONFIGURATION_ERROR` instead. Mock state is persisted to `data/mock_mode_config.json` in Node.js and PHP, but is an in-memory static flag in Java (`MockModeServlet.isMockModeEnabled()`) and .NET (`mockModeEnabled` in `Program.cs`), so it resets on restart there.

4. **`POST /payment-methods` does two jobs and the request shape is not identical across languages.** A body with `id` is an edit (only `nickname` and `isDefault` change); anything else is a create. PHP, Node.js, and .NET expect `payment_token`, `cardDetails`, and flat snake_case customer fields (`first_name`, `street_address`, `billing_zip`, ...). Java expects `paymentToken`, `cardDetails`, and a nested `customerData` object. Java's `PaymentUtils.CustomerData` reads snake_case keys, but `java/src/main/webapp/index.html` sends camelCase ones (`firstName`, `streetAddress`, `billingZip`), so cardholder name, street, and postal code reach the Java charge empty.

## Repository Structure

### PHP (built-in PHP server + Global Payments SDK)
- [`php/router.php`](php/router.php) - router for `php -S`; serves `index.html`, static files, and sends API paths to `index.php`
- [`php/index.php`](php/index.php) - API dispatcher to `health.php`, `config.php`, `payment-methods.php`, `mock-mode.php`
- [`php/PaymentUtils.php`](php/PaymentUtils.php) - reference implementation: `configureSdk()`, `createMultiUseTokenWithCustomer()`, `sanitizePostalCode()`, `determineCardBrandFromType()`
- [`php/config.php`](php/config.php) - restricted `GpApiConfig` and `GpApiService::generateTransactionKey()`
- [`php/payment-methods.php`](php/payment-methods.php) - list, create, and edit handler
- [`php/mock-mode.php`](php/mock-mode.php) - `MockModeConfig` class plus the `/mock-mode` handler
- [`php/JsonStorage.php`](php/JsonStorage.php) - `data/payment_methods.json` storage and `validatePaymentMethod()`
- [`php/MockResponses.php`](php/MockResponses.php), [`php/index.html`](php/index.html)

### Node.js (Express + Global Payments SDK)
- [`nodejs/server.js`](nodejs/server.js) - all routes as inline Express handlers
- [`nodejs/PaymentUtils.js`](nodejs/PaymentUtils.js) - `configureSdk()`, `createMultiUseTokenWithCustomer()`, response helpers
- [`nodejs/MockModeConfig.js`](nodejs/MockModeConfig.js) - `isMockModeEnabled()`, `setMockModeEnabled()`
- [`nodejs/JsonStorage.js`](nodejs/JsonStorage.js), [`nodejs/MockResponses.js`](nodejs/MockResponses.js), [`nodejs/index.html`](nodejs/index.html)
- [`nodejs/data/`](nodejs/data/) - `payment_methods.json` and `mock_mode_config.json` are committed with sample `PMT_...` entries even though `.gitignore` excludes `data/*.json`

### Java (Jakarta Servlet + Global Payments SDK, Cargo/Tomcat)
- [`java/src/main/java/com/globalpayments/example/PaymentMethodsServlet.java`](java/src/main/java/com/globalpayments/example/PaymentMethodsServlet.java) - `/payment-methods`; `doGet()`, `doPost()`, `handleEditPaymentMethod()`
- [`java/src/main/java/com/globalpayments/example/PaymentUtils.java`](java/src/main/java/com/globalpayments/example/PaymentUtils.java) - `configureSdk()`, `createMultiUseTokenWithCustomer()`, `CustomerData`, `CardDetails`
- [`java/src/main/java/com/globalpayments/example/ConfigServlet.java`](java/src/main/java/com/globalpayments/example/ConfigServlet.java), [`HealthServlet.java`](java/src/main/java/com/globalpayments/example/HealthServlet.java), [`MockModeServlet.java`](java/src/main/java/com/globalpayments/example/MockModeServlet.java), [`JsonStorage.java`](java/src/main/java/com/globalpayments/example/JsonStorage.java), [`MockResponses.java`](java/src/main/java/com/globalpayments/example/MockResponses.java)
- [`java/src/main/webapp/index.html`](java/src/main/webapp/index.html) - Java-specific frontend copy (different request body, see Critical Pattern 4)

### .NET (ASP.NET Core minimal API + Global Payments SDK)
- [`dotnet/Program.cs`](dotnet/Program.cs) - `ConfigureGlobalPaymentsSDK()`, `ConfigureEndpoints()`, `HandleCreatePaymentMethodMultiUse()`, `HandleEditPaymentMethodPhpStyle()`
- [`dotnet/PaymentUtils.cs`](dotnet/PaymentUtils.cs) - `CreateMultiUseTokenWithCustomerAsync()`
- [`dotnet/Models.cs`](dotnet/Models.cs) - request/response models; `PaymentMethodData` maps `payment_token` and snake_case customer fields
- [`dotnet/JsonStorage.cs`](dotnet/JsonStorage.cs), [`dotnet/MockResponses.cs`](dotnet/MockResponses.cs), [`dotnet/wwwroot/index.html`](dotnet/wwwroot/index.html)

### Shared
- [`docker-compose.yml`](docker-compose.yml) and [`docker-run.sh`](docker-run.sh) - declare `python` and `go` services that do not exist in this repo, pass `PUBLIC_API_KEY` / `SECRET_API_KEY` that no implementation reads, and a `tests` service whose `Dockerfile.tests` copies a `tests/` folder and `playwright.config.js` that are not in the repo
- [`package.json`](package.json) - root starter-template stub; `npm start` runs `nodejs/server.js`
- There is no root `index.html`. Each language has its own frontend copy; PHP and Node.js are identical, Java and .NET differ.

## API Surface

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/health` | Health payload (fields differ per language) |
| GET | `/config` | Returns `{ accessToken }` for Drop-In UI tokenization |
| GET | `/payment-methods` | Lists stored cards (`id`, `brand`, `last4`, `expiry`, `nickname`, `isDefault`) |
| POST | `/payment-methods` | Creates a card from a single-use token, or edits one when `id` is present |
| GET | `/mock-mode` | Returns `{ isEnabled }` |
| POST | `/mock-mode` | Sets mock mode; body `{"isEnabled": true}` (must be a boolean) |

All responses use the envelope `{success, data, message, timestamp}` plus an error code on failures (`error_code` in PHP, Node.js, and Java; `errorCode` in .NET, which serializes camelCase). There is no charge endpoint: Node.js `/health` advertises `POST /charge` and `php/router.php` lists `charge`, but no handler exists, and the `processPaymentWithSDK()` helpers are never called.

## Environment Variables

```bash
GP_API_APP_ID=your_app_id          # GP-API application ID
GP_API_APP_KEY=your_app_key        # GP-API application key; if empty, Node/PHP/.NET fall back to mock data
GP_API_ENVIRONMENT=sandbox         # Read only by Java and .NET; Node.js and PHP hardcode Environment.TEST
PORT=8000                          # Optional; read by Node.js, .NET, and php/run.sh. Java is fixed by cargo.servlet.port
APP_ENV=development                # Optional; only echoed in Node.js and PHP /health
```

Every language directory has an identical `.env.sample`; copy it to `.env`. The sample ships prefilled sandbox values; replace them with your own. Its comment says "required for Pay by Link", which is a leftover from another sample.

## Test Cards

GP-API sandbox cards:

| Brand | Number | CVV | Expiry |
|-------|--------|-----|--------|
| Visa | 4263970000005262 | 123 | Any future date |
| Mastercard | 5425230000004415 | 123 | Any future date |

The frontend's test-card picker lists different numbers (Visa 4012002000060016, Mastercard 2223000010005780 and 5473500000000014, plus Discover, Amex, JCB), and the Java and .NET READMEs repeat those. Get sandbox credentials at [developer.globalpayments.com](https://developer.globalpayments.com).

## Architecture Summary

**Tokenize:** page load -> `GET /config` -> `generateTransactionKey()` with `PMT_POST_Create_Single` -> Drop-In UI iframe -> `token-success` gives `paymentReference` and `details` -> `POST /payment-methods`.

**Store:** server -> `$0.01` USD charge with `withRequestMultiUseToken(true)` and billing address -> `response.token` -> appended to `data/payment_methods.json` with brand, last4, expiry, nickname, `isDefault`, and customer data.

## Security Notes

No authentication on any endpoint, CORS open to `*`, stored tokens and customer PII in a plain JSON file, credentials in `.env`. Node.js and PHP always target the sandbox. For production: add auth, move storage to a database, use a secrets manager, enable HTTPS, and decide whether the $0.01 charge should be reversed.

## How to Run

```bash
cd php && ./run.sh       # PHP: :8000 (composer install, php -S 0.0.0.0:$PORT router.php)
cd nodejs && ./run.sh    # Node.js: :8000 (npm install, npm start)
cd java && ./run.sh      # Java: :8000 (mvn clean package cargo:run)
cd dotnet && ./run.sh    # .NET: :8000 (dotnet restore, dotnet run; honors PORT)
```

`docker-compose up` fails because the `python` and `go` build contexts do not exist. Use the per-language `./run.sh`. Creating a real card needs a browser: the Drop-In UI iframe (`https://js.globalpay.com/v1/globalpayments.js`) produces the `PMT_...` token.

## How to Verify

```bash
curl http://localhost:8000/health
# Expected: {"success":true,"data":{"status":"healthy",...},...}

curl http://localhost:8000/config
# Expected: {"success":true,"data":{"accessToken":"..."},...}  (needs valid credentials)

curl -X POST http://localhost:8000/mock-mode -H "Content-Type: application/json" -d '{"isEnabled":true}'
# Expected: {"success":true,"data":{"isEnabled":true},"message":"Mock mode enabled successfully",...}

# Create in mock mode (PHP, Node.js, .NET body shape)
curl -X POST http://localhost:8000/payment-methods -H "Content-Type: application/json" \
  -d '{"payment_token":"PMT_test","cardDetails":{"cardType":"visa","cardLast4":"5262","expiryMonth":"12","expiryYear":"2028"},"first_name":"Jane","last_name":"Doe","billing_zip":"47130","country":"US"}'
# Java: send "paymentToken" and a nested "customerData" object instead
# Expected: {"success":true,"data":{"id":"pm_...","brand":"Visa","last4":"5262",...,"mockMode":true},...}

curl http://localhost:8000/payment-methods
# Expected: {"success":true,"data":[{"id":"pm_...","type":"card","last4":"5262",...}],...}

# Edit
curl -X POST http://localhost:8000/payment-methods -H "Content-Type: application/json" -d '{"id":"pm_...","nickname":"Work card","isDefault":true}'
```

Outside mock mode, `POST /payment-methods` needs a real `PMT_...` token from the browser flow.

## Making Changes

All four implementations are meant to expose identical behavior. A change to one must be applied to all, each language in a separate commit. The four `index.html` copies are independent and must be updated together, keeping Java's request shape in mind. Do not edit `docker-compose.yml` or `docker-run.sh` in isolation; they are shared and already out of sync with the tree. Python and Go are absent; do not add them without explicit instruction.

## SDK Versions

- **PHP**: `globalpayments/php-sdk` ^13.1, `vlucas/phpdotenv` ^5.5
- **Node.js**: `globalpayments-api` ^3.10.6, `express` ^4.18.2, `dotenv` ^16.3.1 (ES modules)
- **Java**: `globalpayments-sdk` (com.heartlandpaymentsystems) 14.2.20, `dotenv-java` 3.0.0, Jakarta Servlet 5.0.0
- **.NET**: `GlobalPayments.Api` 9.0.16, `DotEnv.Net` 3.2.1, net9.0
- **Browser**: `https://js.globalpay.com/v1/globalpayments.js`
