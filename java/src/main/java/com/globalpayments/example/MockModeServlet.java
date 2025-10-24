package com.globalpayments.example;

import com.google.gson.Gson;
import jakarta.servlet.ServletException;
import jakarta.servlet.annotation.WebServlet;
import jakarta.servlet.http.HttpServlet;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;

import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * Mock Mode endpoint
 * 
 * GET /mock-mode - Get mock mode status
 * POST /mock-mode - Toggle mock mode
 */
@WebServlet(name = "MockModeServlet", urlPatterns = {"/mock-mode"})
public class MockModeServlet extends HttpServlet {
    
    private static final Gson gson = new Gson();
    
    // Global mock mode configuration
    private static boolean mockModeEnabled = false;
    
    public static boolean isMockModeEnabled() {
        return mockModeEnabled;
    }
    
    public static void setMockModeEnabled(boolean enabled) {
        mockModeEnabled = enabled;
    }
    
    @Override
    protected void doGet(HttpServletRequest request, HttpServletResponse response)
            throws ServletException, IOException {
        
        handleCORS(response);
        
        System.out.println("📊 MOCK MODE STATUS - Current state: " + getMockModeStatus());
        
        Map<String, Object> mockModeConfig = new HashMap<>();
        mockModeConfig.put("isEnabled", mockModeEnabled);
        
        Map<String, Object> responseData = new HashMap<>();
        responseData.put("success", true);
        responseData.put("data", mockModeConfig);
        responseData.put("message", "Mock mode is " + getMockModeText());
        responseData.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        
        response.getWriter().write(gson.toJson(responseData));
    }
    
    @Override
    protected void doPost(HttpServletRequest request, HttpServletResponse response)
            throws ServletException, IOException {
        
        handleCORS(response);
        
        try {
            // Parse JSON input
            String jsonString = request.getReader().lines().collect(Collectors.joining());
            @SuppressWarnings("unchecked")
            Map<String, Object> data = gson.fromJson(jsonString, Map.class);
            
            Boolean isEnabled = (Boolean) data.get("isEnabled");
            if (isEnabled == null) {
                sendErrorResponse(response, 400, "Invalid JSON format", "VALIDATION_ERROR");
                return;
            }
            
            boolean previousState = mockModeEnabled;
            mockModeEnabled = isEnabled;
            
            System.out.println("⚙️  MOCK MODE TOGGLE - Changed from " + 
                getMockModeStatusFor(previousState) + " to " + getMockModeStatusFor(mockModeEnabled));
            System.out.println("   ⏰ Timestamp: " + LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
            System.out.println("   🎛️  New State: " + getMockModeDescription());
            
            Map<String, Object> mockModeConfig = new HashMap<>();
            mockModeConfig.put("isEnabled", mockModeEnabled);
            
            Map<String, Object> responseData = new HashMap<>();
            responseData.put("success", true);
            responseData.put("data", mockModeConfig);
            responseData.put("message", "Mock mode " + getMockModeText() + " successfully");
            responseData.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
            
            response.getWriter().write(gson.toJson(responseData));
            
        } catch (Exception e) {
            sendErrorResponse(response, 400, "Invalid JSON format", "VALIDATION_ERROR");
        }
    }
    
    @Override
    protected void doOptions(HttpServletRequest request, HttpServletResponse response)
            throws ServletException, IOException {
        handleCORS(response);
        response.setStatus(HttpServletResponse.SC_OK);
    }
    
    private void handleCORS(HttpServletResponse response) {
        response.setContentType("application/json");
        response.setCharacterEncoding("UTF-8");
        response.setHeader("Access-Control-Allow-Origin", "*");
        response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
    }
    
    private void sendErrorResponse(HttpServletResponse response, int statusCode, String message, String errorCode) 
            throws IOException {
        response.setStatus(statusCode);
        
        Map<String, Object> errorResponse = new HashMap<>();
        errorResponse.put("success", false);
        errorResponse.put("message", message);
        errorResponse.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));
        
        if (errorCode != null) {
            errorResponse.put("error_code", errorCode);
        }
        
        response.getWriter().write(gson.toJson(errorResponse));
    }
    
    // Helper functions for mock mode
    private String getMockModeStatus() {
        return mockModeEnabled ? "🟡 ENABLED" : "🟢 DISABLED";
    }
    
    private String getMockModeStatusFor(boolean enabled) {
        return enabled ? "🟡 ENABLED" : "🟢 DISABLED";
    }
    
    private String getMockModeText() {
        return mockModeEnabled ? "enabled" : "disabled";
    }
    
    private String getMockModeDescription() {
        return mockModeEnabled ? "Mock mode will be used for all operations" : "Live API will be attempted first";
    }
}