using GlobalPayments.Api;
using GlobalPayments.Api.Entities;
using GlobalPayments.Api.Entities.Enums;
using GlobalPayments.Api.PaymentMethods;
using GlobalPayments.Api.ServiceConfigs;
using GlobalPayments.Api.Services;
using dotenv.net;
using System.Text.Json;
using GpEnvironment = GlobalPayments.Api.Entities.Environment;

namespace CardPaymentSample;

/// <summary>
/// Wallet Management Application
///
/// Complete REST API implementation with payment method management,
/// mock mode support, and comprehensive error handling.
/// </summary>
public class Program
{
    private static bool mockModeEnabled = false;
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Main(string[] args)
    {
        // Load environment variables from .env file
        DotEnv.Load();

        var builder = WebApplication.CreateBuilder(args);
        
        // Add CORS support
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure static file serving
        app.UseDefaultFiles();
        app.UseStaticFiles();
        
        // Enable CORS
        app.UseCors("AllowAll");
        
        // Configure the SDK on startup
        ConfigureGlobalPaymentsSDK();

        // Configure all endpoints
        ConfigureEndpoints(app);
        
        var port = System.Environment.GetEnvironmentVariable("PORT") ?? "8000";
        app.Urls.Add($"http://0.0.0.0:{port}");

        Console.WriteLine($".NET Server starting on port {port}");

        app.Run();
    }

    private static void ConfigureGlobalPaymentsSDK()
    {
        try
        {
            var environment = System.Environment.GetEnvironmentVariable("GP_API_ENVIRONMENT") ?? "sandbox";
            var isProduction = environment.ToLower() == "production";

            var config = new GpApiConfig
            {
                AppId = System.Environment.GetEnvironmentVariable("GP_API_APP_ID"),
                AppKey = System.Environment.GetEnvironmentVariable("GP_API_APP_KEY"),
                Environment = isProduction ? GpEnvironment.PRODUCTION : GpEnvironment.TEST,
                Channel = Channel.CardNotPresent,
                Country = "US"
            };

            ServicesContainer.ConfigureService(config);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SDK configuration failed: {ex.Message}");
        }
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        // Health check endpoint
        app.MapGet("/health", () =>
        {
            return Results.Ok(new ApiResponse<HealthData>
            {
                Success = true,
                Data = new HealthData
                {
                    Status = "healthy",
                    Timestamp = DateTime.UtcNow,
                    Service = "wallet-management-dotnet",
                    Version = "1.0.0"
                },
                Message = "System is healthy",
                Timestamp = DateTime.UtcNow
            });
        });

        // Generate GP API access token for frontend
        app.MapGet("/config", () =>
        {
            try
            {
                var environment = System.Environment.GetEnvironmentVariable("GP_API_ENVIRONMENT") ?? "sandbox";
                var isProduction = environment.ToLower() == "production";

                var config = new GpApiConfig
                {
                    AppId = System.Environment.GetEnvironmentVariable("GP_API_APP_ID"),
                    AppKey = System.Environment.GetEnvironmentVariable("GP_API_APP_KEY"),
                    Environment = isProduction ? GpEnvironment.PRODUCTION : GpEnvironment.TEST,
                    Channel = Channel.CardNotPresent,
                    Country = "US",
                    Permissions = new[] { "PMT_POST_Create_Single" }
                };

                var accessTokenInfo = GpApiService.GenerateTransactionKey(config);

                if (string.IsNullOrEmpty(accessTokenInfo.Token))
                {
                    throw new Exception("Failed to generate session token");
                }

                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { AccessToken = accessTokenInfo.Token },
                    Message = "Configuration retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Configuration error: {ex.Message}");
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error loading configuration: {ex.Message}",
                    ErrorCode = "CONFIG_ERROR",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }
        });

        // Mock mode endpoints
        app.MapGet("/mock-mode", () =>
        {
            return Results.Ok(new ApiResponse<MockModeConfig>
            {
                Success = true,
                Data = new MockModeConfig { IsEnabled = mockModeEnabled },
                Message = $"Mock mode is {(mockModeEnabled ? "enabled" : "disabled")}",
                Timestamp = DateTime.UtcNow
            });
        });

        app.MapPost("/mock-mode", async (HttpContext context) =>
        {
            try
            {
                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<MockModeConfig>(json, jsonOptions);
                
                if (request == null)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request body",
                        ErrorCode = "VALIDATION_ERROR",
                        Timestamp = DateTime.UtcNow
                    });
                }

                mockModeEnabled = request.IsEnabled;

                return Results.Ok(new ApiResponse<MockModeConfig>
                {
                    Success = true,
                    Data = new MockModeConfig { IsEnabled = mockModeEnabled },
                    Message = $"Mock mode {(mockModeEnabled ? "enabled" : "disabled")} successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                return Results.BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid JSON format",
                    ErrorCode = "VALIDATION_ERROR",
                    Timestamp = DateTime.UtcNow
                });
            }
        });

        app.MapGet("/payment-methods", async () =>
        {
            try
            {
                var methods = await JsonStorage.LoadPaymentMethodsAsync();
                var formattedMethods = methods.Select(method => new
                {
                    id = method.Id,
                    type = "card",
                    last4 = method.Last4,
                    brand = method.CardBrand,
                    expiry = $"{method.ExpiryMonth}/{method.ExpiryYear}",
                    isDefault = method.IsDefault,
                    nickname = method.Nickname ?? ""
                }).ToList();
                
                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = formattedMethods,
                    Message = "Payment methods retrieved successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving payment methods: {ex.Message}");
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve payment methods",
                    ErrorCode = "SERVER_ERROR",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }
        });

        app.MapPost("/payment-methods", async (HttpContext context) =>
        {
            try
            {
                var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<PaymentMethodData>(json, jsonOptions);
                
                if (data == null)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid request body",
                        ErrorCode = "VALIDATION_ERROR",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (!string.IsNullOrEmpty(data.Id))
                {
                    return await HandleEditPaymentMethodPhpStyle(data);
                }

                if (string.IsNullOrEmpty(data.PaymentToken))
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Missing required payment_token",
                        ErrorCode = "VALIDATION_ERROR",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (data.CardDetails == null)
                {
                    return Results.BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Missing required cardDetails",
                        ErrorCode = "VALIDATION_ERROR",
                        Timestamp = DateTime.UtcNow
                    });
                }

                return await HandleCreatePaymentMethodMultiUse(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Payment methods error: {ex.Message}");
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "SERVER_ERROR",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }
        });

    }

    private static async Task<IResult> HandleEditPaymentMethodPhpStyle(PaymentMethodData data)
    {
        try
        {
            var existingMethod = await JsonStorage.FindPaymentMethodAsync(data.Id!);
            if (existingMethod == null)
            {
                return Results.NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Payment method not found",
                    ErrorCode = "NOT_FOUND",
                    Timestamp = DateTime.UtcNow
                });
            }

            var editData = new PaymentMethodEditData
            {
                Nickname = data.Nickname,
                IsDefault = data.IsDefault
            };

            var updatedMethod = await JsonStorage.UpdatePaymentMethodAsync(data.Id!, editData);
            if (updatedMethod == null)
            {
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to update payment method",
                    ErrorCode = "UPDATE_ERROR",
                    Timestamp = DateTime.UtcNow
                }, statusCode: 500);
            }

            var response = new
            {
                id = updatedMethod.Id,
                type = "card",
                last4 = updatedMethod.Last4,
                brand = updatedMethod.CardBrand,
                expiry = $"{updatedMethod.ExpiryMonth}/{updatedMethod.ExpiryYear}",
                nickname = updatedMethod.Nickname ?? "",
                isDefault = updatedMethod.IsDefault,
                updatedAt = updatedMethod.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return Results.Ok(new ApiResponse<object>
            {
                Success = true,
                Data = response,
                Message = "Payment method updated successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating payment method: {ex.Message}");
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Payment method update failed",
                ErrorCode = "SERVER_ERROR",
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }


    private static async Task<IResult> HandleCreatePaymentMethodMultiUse(PaymentMethodData data)
    {
        try
        {
            var paymentToken = data.PaymentToken!;
            var cardDetails = data.CardDetails!;
            var customerData = data.CustomerData ?? new CustomerData
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = data.Email,
                Phone = data.Phone,
                StreetAddress = data.StreetAddress,
                City = data.City,
                State = data.State,
                BillingZip = data.BillingZip,
                Country = data.Country
            };

            MultiUseTokenResult? multiUseTokenData = null;
            var finalToken = paymentToken;

            if (!mockModeEnabled && !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GP_API_APP_KEY")))
            {
                try
                {
                    multiUseTokenData = await PaymentUtils.CreateMultiUseTokenWithCustomerAsync(paymentToken, customerData, cardDetails);
                    finalToken = multiUseTokenData.MultiUseToken;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Multi-use token creation error: {ex.Message}");
                    mockModeEnabled = true;
                }
            }

            if (mockModeEnabled || multiUseTokenData == null)
            {
                var brand = PaymentUtils.DetermineCardBrandFromType(cardDetails.CardType ?? "");
                multiUseTokenData = new MultiUseTokenResult
                {
                    MultiUseToken = paymentToken,
                    Brand = brand,
                    Last4 = cardDetails.CardLast4 ?? "",
                    ExpiryMonth = cardDetails.ExpiryMonth ?? "",
                    ExpiryYear = cardDetails.ExpiryYear ?? "",
                    CustomerData = customerData
                };
            }

            var storedData = new StoredPaymentMethodData
            {
                StoredPaymentToken = finalToken,
                CardBrand = multiUseTokenData.Brand,
                Last4 = multiUseTokenData.Last4,
                ExpiryMonth = multiUseTokenData.ExpiryMonth,
                ExpiryYear = multiUseTokenData.ExpiryYear,
                Nickname = data.Nickname ?? $"{multiUseTokenData.Brand} ending in {multiUseTokenData.Last4}",
                IsDefault = data.IsDefault,
                CustomerData = customerData
            };

            var savedMethod = await JsonStorage.AddPaymentMethodAsync(storedData);

            var response = new
            {
                id = savedMethod.Id,
                storedPaymentToken = finalToken,
                type = "card",
                last4 = multiUseTokenData.Last4,
                brand = multiUseTokenData.Brand,
                expiry = $"{multiUseTokenData.ExpiryMonth}/{multiUseTokenData.ExpiryYear}",
                nickname = savedMethod.Nickname,
                isDefault = savedMethod.IsDefault,
                mockMode = mockModeEnabled
            };

            return Results.Ok(new ApiResponse<object>
            {
                Success = true,
                Data = response,
                Message = "Payment method created and saved successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating payment method: {ex.Message}");
            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Message = "Payment method creation failed",
                ErrorCode = "SERVER_ERROR",
                Timestamp = DateTime.UtcNow
            }, statusCode: 500);
        }
    }


}

