package com.globalpayments.example;

import com.global.api.entities.enums.Channel;
import com.global.api.entities.enums.Environment;
import com.global.api.serviceConfigs.GpApiConfig;
import com.global.api.services.GpApiService;
import com.google.gson.Gson;
import io.github.cdimascio.dotenv.Dotenv;
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

/**
 * Configuration Endpoint - GP API
 *
 * GET /config - Generate GP API access token for frontend tokenization
 */
@WebServlet(name = "ConfigServlet", urlPatterns = {"/config"})
public class ConfigServlet extends HttpServlet {

    private static final Gson gson = new Gson();
    private static final Dotenv dotenv = Dotenv.configure().ignoreIfMissing().load();

    @Override
    protected void doGet(HttpServletRequest request, HttpServletResponse response)
            throws ServletException, IOException {

        response.setContentType("application/json");
        response.setCharacterEncoding("UTF-8");

        // Add CORS headers
        response.setHeader("Access-Control-Allow-Origin", "*");
        response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

        try {
            String environment = dotenv.get("GP_API_ENVIRONMENT", "sandbox");
            boolean isProduction = "production".equalsIgnoreCase(environment);

            // Configure GP API for session token generation
            GpApiConfig config = new GpApiConfig();
            config.setAppId(dotenv.get("GP_API_APP_ID"));
            config.setAppKey(dotenv.get("GP_API_APP_KEY"));
            config.setEnvironment(isProduction ? Environment.PRODUCTION : Environment.TEST);
            config.setChannel(Channel.CardNotPresent);
            config.setCountry("US");
            config.setPermissions(new String[]{"PMT_POST_Create_Single"});

            // Generate session token for client-side use
            var accessTokenInfo = GpApiService.generateTransactionKey(config);

            if (accessTokenInfo == null || accessTokenInfo.getAccessToken() == null || accessTokenInfo.getAccessToken().isEmpty()) {
                throw new Exception("Failed to generate session token");
            }

            String accessToken = accessTokenInfo.getAccessToken();

            System.out.println("Session token generated successfully: " +
                accessToken.substring(0, Math.min(8, accessToken.length())) + "...");

            Map<String, Object> data = new HashMap<>();
            data.put("accessToken", accessToken);

            Map<String, Object> responseData = new HashMap<>();
            responseData.put("success", true);
            responseData.put("data", data);
            responseData.put("message", "Configuration retrieved successfully");
            responseData.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));

            response.getWriter().write(gson.toJson(responseData));

        } catch (Exception e) {
            System.err.println("Configuration error: " + e.getMessage());

            Map<String, Object> errorResponse = new HashMap<>();
            errorResponse.put("success", false);
            errorResponse.put("message", "Error loading configuration: " + e.getMessage());
            errorResponse.put("errorCode", "CONFIG_ERROR");
            errorResponse.put("timestamp", LocalDateTime.now().format(DateTimeFormatter.ISO_LOCAL_DATE_TIME));

            response.setStatus(HttpServletResponse.SC_INTERNAL_SERVER_ERROR);
            response.getWriter().write(gson.toJson(errorResponse));
        }
    }

    @Override
    protected void doOptions(HttpServletRequest request, HttpServletResponse response)
            throws ServletException, IOException {

        response.setHeader("Access-Control-Allow-Origin", "*");
        response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.setStatus(HttpServletResponse.SC_OK);
    }
}
