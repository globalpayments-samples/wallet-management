/**
 * Mock Mode Configuration Class
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const CONFIG_FILE = path.join(__dirname, 'data', 'mock_mode_config.json');

/**
 * Get mock mode status
 */
export function isMockModeEnabled() {
    if (!fs.existsSync(CONFIG_FILE)) {
        return false; // Default to disabled
    }

    try {
        const content = fs.readFileSync(CONFIG_FILE, 'utf8');
        const config = JSON.parse(content);
        return config.isEnabled || false;
    } catch (error) {
        console.error('Error reading mock mode config:', error.message);
        return false;
    }
}

/**
 * Set mock mode status
 */
export function setMockModeEnabled(enabled) {
    // Ensure data directory exists
    const dataDir = path.dirname(CONFIG_FILE);
    if (!fs.existsSync(dataDir)) {
        fs.mkdirSync(dataDir, { recursive: true, mode: 0o755 });
    }

    const config = {
        isEnabled: enabled,
        lastUpdated: new Date().toISOString()
    };

    try {
        const json = JSON.stringify(config, null, 2);
        fs.writeFileSync(CONFIG_FILE, json, 'utf8');
        return true;
    } catch (error) {
        console.error('Error writing mock mode config:', error.message);
        return false;
    }
}

/**
 * Get mock mode status text
 */
export function getMockModeStatus() {
    return isMockModeEnabled() ? '🟡 ENABLED' : '🟢 DISABLED';
}

/**
 * Get mock mode text
 */
export function getMockModeText() {
    return isMockModeEnabled() ? 'enabled' : 'disabled';
}

/**
 * Get mock mode description
 */
export function getMockModeDescription() {
    return isMockModeEnabled()
        ? 'Mock mode will be used for all operations'
        : 'Live API will be attempted first';
}
