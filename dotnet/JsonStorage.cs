using System.Text.Json;

namespace CardPaymentSample;

/// <summary>
/// Simple JSON-based data storage for payment methods
/// </summary>
public static class JsonStorage
{
    private const string DataDir = "data";
    private const string PaymentMethodsFile = "data/payment_methods.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Ensure data directory exists
    /// </summary>
    private static void EnsureDataDir()
    {
        if (!Directory.Exists(DataDir))
        {
            Directory.CreateDirectory(DataDir);
        }
    }

    /// <summary>
    /// Load payment methods from JSON file
    /// </summary>
    public static async Task<List<PaymentMethod>> LoadPaymentMethodsAsync()
    {
        EnsureDataDir();

        if (!File.Exists(PaymentMethodsFile))
        {
            return new List<PaymentMethod>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(PaymentMethodsFile);
            return JsonSerializer.Deserialize<List<PaymentMethod>>(json, JsonOptions) ?? new List<PaymentMethod>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading payment methods: {ex.Message}");
            return new List<PaymentMethod>();
        }
    }

    /// <summary>
    /// Save payment methods to JSON file
    /// </summary>
    public static async Task SavePaymentMethodsAsync(List<PaymentMethod> methods)
    {
        EnsureDataDir();

        try
        {
            var json = JsonSerializer.Serialize(methods, JsonOptions);
            await File.WriteAllTextAsync(PaymentMethodsFile, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error saving payment methods: {ex.Message}");
        }
    }

    /// <summary>
    /// Add a new payment method
    /// </summary>
    public static async Task<PaymentMethod> AddPaymentMethodAsync(StoredPaymentMethodData data)
    {
        var methods = await LoadPaymentMethodsAsync();

        // Generate unique ID
        var id = $"pm_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString()[^9..]}";

        // Create payment method object
        var paymentMethod = new PaymentMethod
        {
            Id = id,
            StoredPaymentToken = data.StoredPaymentToken,
            CardBrand = data.CardBrand,
            Last4 = data.Last4,
            ExpiryMonth = data.ExpiryMonth,
            ExpiryYear = data.ExpiryYear,
            Nickname = data.Nickname,
            IsDefault = data.IsDefault,
            CustomerData = data.CustomerData,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Handle default payment method
        if (data.IsDefault)
        {
            // Remove default from all existing methods
            foreach (var method in methods)
            {
                method.IsDefault = false;
            }
        }
        else if (methods.Count == 0)
        {
            // Make first method default
            paymentMethod.IsDefault = true;
        }

        methods.Add(paymentMethod);
        await SavePaymentMethodsAsync(methods);

        return paymentMethod;
    }

    /// <summary>
    /// Find payment method by ID
    /// </summary>
    public static async Task<PaymentMethod?> FindPaymentMethodAsync(string id)
    {
        var methods = await LoadPaymentMethodsAsync();
        return methods.FirstOrDefault(m => m.Id == id);
    }

    /// <summary>
    /// Get all payment methods formatted for display
    /// </summary>
    public static async Task<List<FormattedPaymentMethod>> GetFormattedPaymentMethodsAsync()
    {
        var methods = await LoadPaymentMethodsAsync();

        return methods.Select(method => new FormattedPaymentMethod
        {
            Id = method.Id,
            Brand = method.CardBrand,
            Last4 = method.Last4,
            Expiry = $"{method.ExpiryMonth}/{method.ExpiryYear}",
            Nickname = method.Nickname,
            IsDefault = method.IsDefault
        }).ToList();
    }

    /// <summary>
    /// Update an existing payment method (nickname and default status only)
    /// </summary>
    public static async Task<PaymentMethod?> UpdatePaymentMethodAsync(string id, PaymentMethodEditData editData)
    {
        var methods = await LoadPaymentMethodsAsync();
        var method = methods.FirstOrDefault(m => m.Id == id);
        
        if (method == null)
        {
            return null;
        }

        // Update nickname
        method.Nickname = editData.Nickname;
        method.UpdatedAt = DateTime.UtcNow;

        // Handle default payment method change
        if (editData.IsDefault && !method.IsDefault)
        {
            // Remove default from all existing methods
            foreach (var m in methods)
            {
                m.IsDefault = false;
            }
            method.IsDefault = true;
        }
        else if (!editData.IsDefault && method.IsDefault)
        {
            method.IsDefault = false;
        }

        await SavePaymentMethodsAsync(methods);
        return method;
    }

    /// <summary>
    /// Set a payment method as default (and remove default from others)
    /// </summary>
    public static async Task<bool> SetDefaultPaymentMethodAsync(string id)
    {
        var methods = await LoadPaymentMethodsAsync();
        var targetMethod = methods.FirstOrDefault(m => m.Id == id);
        
        if (targetMethod == null)
        {
            return false;
        }

        // Remove default from all methods
        foreach (var method in methods)
        {
            method.IsDefault = false;
        }

        // Set target method as default
        targetMethod.IsDefault = true;
        targetMethod.UpdatedAt = DateTime.UtcNow;

        await SavePaymentMethodsAsync(methods);
        return true;
    }
}