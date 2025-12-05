#!/bin/bash

# Exit on error
set -e

# Install requirements
composer install

# Create data directory if it doesn't exist
mkdir -p data

# Start the server with router
PORT="${PORT:-8000}"
echo "Starting server on http://localhost:$PORT"
echo "Press Ctrl+C to stop the server"
php -S 0.0.0.0:$PORT router.php
