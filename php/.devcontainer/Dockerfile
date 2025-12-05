FROM mcr.microsoft.com/devcontainers/php:8.3

RUN ( curl -sSLf https://github.com/mlocati/docker-php-extension-installer/releases/latest/download/install-php-extensions -o - || echo 'return 1' ) | sh -s \
      intl zip dom curl

# Install Composer
COPY --from=composer:latest /usr/bin/composer /usr/bin/composer
