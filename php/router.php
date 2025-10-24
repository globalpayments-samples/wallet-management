<?php

declare(strict_types=1);

/**
 * Router for PHP built-in server
 * 
 * This router handles:
 * - Serving index.html for root requests
 * - Routing API calls to index.php
 * - Serving static files directly
 */

$requestUri = $_SERVER['REQUEST_URI'];
$path = parse_url($requestUri, PHP_URL_PATH);

// Remove leading slash and normalize path
$path = ltrim($path, '/');

// Serve index.html for root requests
if (empty($path) || $path === '/') {
    if (file_exists('index.html')) {
        header('Content-Type: text/html');
        readfile('index.html');
        return true;
    }
    return false;
}

// Serve static files directly (CSS, JS, images, etc.)
$staticExtensions = ['css', 'js', 'png', 'jpg', 'jpeg', 'gif', 'ico', 'svg', 'woff', 'woff2', 'ttf'];
$pathInfo = pathinfo($path);
if (isset($pathInfo['extension']) && in_array(strtolower($pathInfo['extension']), $staticExtensions)) {
    if (file_exists($path)) {
        return false; // Let PHP serve the file with appropriate MIME type
    }
    http_response_code(404);
    return true;
}

// Route API calls to index.php (our API router)
$apiPaths = ['health', 'payment-methods', 'charge', 'config', 'mock-mode'];
if (in_array($path, $apiPaths)) {
    require_once 'index.php';
    return true;
}

// Handle 404 for unknown paths
http_response_code(404);
echo json_encode([
    'success' => false,
    'message' => 'Endpoint not found',
    'path' => $path
]);
return true;