package com.globalpayments.example;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.reflect.TypeToken;

import java.io.*;
import java.lang.reflect.Type;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;

/**
 * Simple JSON-based data storage for payment methods
 */
public class JsonStorage {
    
    private static final String DATA_DIR = "data";
    private static final String PAYMENT_METHODS_FILE = DATA_DIR + "/payment_methods.json";
    private static final Gson gson = new GsonBuilder().setPrettyPrinting().create();
    
    static {
        // Ensure data directory exists
        try {
            Path dataPath = Paths.get(DATA_DIR);
            if (!Files.exists(dataPath)) {
                Files.createDirectories(dataPath);
            }
        } catch (IOException e) {
            System.err.println("Failed to create data directory: " + e.getMessage());
        }
    }
    
    /**
     * Load payment methods from JSON file
     */
    public static List<Map<String, Object>> loadPaymentMethods() {
        File file = new File(PAYMENT_METHODS_FILE);
        if (!file.exists()) {
            return new ArrayList<>();
        }
        
        try (FileReader reader = new FileReader(file)) {
            Type listType = new TypeToken<List<Map<String, Object>>>(){}.getType();
            List<Map<String, Object>> methods = gson.fromJson(reader, listType);
            return methods != null ? methods : new ArrayList<>();
        } catch (IOException e) {
            System.err.println("Error loading payment methods: " + e.getMessage());
            return new ArrayList<>();
        }
    }
    
    /**
     * Save payment methods to JSON file
     */
    public static void savePaymentMethods(List<Map<String, Object>> methods) {
        try (FileWriter writer = new FileWriter(PAYMENT_METHODS_FILE)) {
            gson.toJson(methods, writer);
        } catch (IOException e) {
            System.err.println("Error saving payment methods: " + e.getMessage());
        }
    }
    
    /**
     * Add a new payment method
     */
    public static Map<String, Object> addPaymentMethod(Map<String, Object> data) {
        List<Map<String, Object>> methods = loadPaymentMethods();
        
        // Generate unique ID
        String id = "pm_" + UUID.randomUUID().toString();
        
        // Create payment method object
        Map<String, Object> paymentMethod = new HashMap<>();
        paymentMethod.put("id", id);
        paymentMethod.put("storedPaymentToken", data.get("storedPaymentToken"));
        paymentMethod.put("cardBrand", data.get("cardBrand"));
        paymentMethod.put("last4", data.get("last4"));
        paymentMethod.put("expiry", data.get("expiry"));
        paymentMethod.put("nickname", data.get("nickname"));
        paymentMethod.put("isDefault", data.get("isDefault"));
        paymentMethod.put("createdAt", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        paymentMethod.put("updatedAt", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        
        // Handle default payment method
        boolean isDefault = Boolean.TRUE.equals(data.get("isDefault"));
        if (isDefault) {
            // Remove default from all existing methods
            for (Map<String, Object> method : methods) {
                method.put("isDefault", false);
            }
        } else if (methods.isEmpty()) {
            // Make first method default
            paymentMethod.put("isDefault", true);
        }
        
        methods.add(paymentMethod);
        savePaymentMethods(methods);
        
        return paymentMethod;
    }
    
    /**
     * Find payment method by ID
     */
    public static Map<String, Object> findPaymentMethod(String id) {
        List<Map<String, Object>> methods = loadPaymentMethods();
        return methods.stream()
                .filter(method -> id.equals(method.get("id")))
                .findFirst()
                .orElse(null);
    }
    
    /**
     * Get all payment methods formatted for display
     */
    public static List<Map<String, Object>> getFormattedPaymentMethods() {
        List<Map<String, Object>> methods = loadPaymentMethods();
        List<Map<String, Object>> formatted = new ArrayList<>();
        
        for (Map<String, Object> method : methods) {
            Map<String, Object> formattedMethod = new HashMap<>();
            formattedMethod.put("id", method.get("id"));
            formattedMethod.put("brand", method.get("cardBrand"));
            formattedMethod.put("last4", method.get("last4"));
            formattedMethod.put("expiry", method.get("expiry"));
            formattedMethod.put("nickname", method.get("nickname"));
            formattedMethod.put("isDefault", method.get("isDefault"));
            formatted.add(formattedMethod);
        }
        
        return formatted;
    }
    
    /**
     * Update payment method
     */
    public static Map<String, Object> updatePaymentMethod(String id, Map<String, Object> updateData) {
        List<Map<String, Object>> methods = loadPaymentMethods();
        
        Map<String, Object> method = methods.stream()
                .filter(m -> id.equals(m.get("id")))
                .findFirst()
                .orElse(null);
        
        if (method == null) {
            throw new RuntimeException("Payment method not found");
        }
        
        // Update the method
        method.putAll(updateData);
        method.put("updatedAt", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        
        savePaymentMethods(methods);
        return method;
    }
    
    /**
     * Set a payment method as default (removes default from all others)
     */
    public static void setDefaultPaymentMethod(String id) {
        List<Map<String, Object>> methods = loadPaymentMethods();
        boolean found = false;
        
        for (Map<String, Object> method : methods) {
            if (id.equals(method.get("id"))) {
                method.put("isDefault", true);
                method.put("updatedAt", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
                found = true;
            } else if (Boolean.TRUE.equals(method.get("isDefault"))) {
                method.put("isDefault", false);
                method.put("updatedAt", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
            }
        }
        
        if (!found) {
            throw new RuntimeException("Payment method not found");
        }
        
        savePaymentMethods(methods);
    }
}