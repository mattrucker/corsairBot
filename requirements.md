# Corsair Bot: Program Requirements (v2)

## I. Configuration
### A. Configuration File (`config.json`)
* **Type:** JSON
* **Location:** Mounted into Docker container via a `/config` volume.
### B. Content of `config.json`
    1.  `region`: (String) Corsair website region (e.g., "us", "de", "ca").
        * _Purpose:_ Constructs base URLs like `https://www.corsair.com/{region}/`.
    2.  `corsair_account`: (Object)
        * `username`: (String) Login username.
        * `password`: (String) Login password.
    3.  `payment_details`: (Object)
        * `card_number`: (String) Credit card number.
        * `expiry_date`: (String) Credit card expiration date (format: "MM/YY").
        * `cvv`: (String) Credit card security code.
        * `name_on_card`: (String) Full name as on credit card.
    4.  `shipping_address`: (Object) Address for purchases.
        * `first_name`: (String)
        * `last_name`: (String)
        * `address_line_1`: (String)
        * `address_line_2`: (String, optional)
        * `city`: (String)
        * `state_province`: (String) State/Province code (e.g., "CA", "ON").
        * `zip_postal_code`: (String)
        * `country_code`: (String) Two-letter country code (e.g., "US", "CA").
        * `phone_number`: (String)
    5.  `skus_to_monitor`: (Array of Objects) Each object:
        * `sku_id`: (String) Product SKU.
        * `name`: (String) User-friendly name for logging.
        * `target_quantity`: (Integer) Desired number of units to purchase for this SKU.
        * `polling_interval_seconds`: (Integer, optional) SKU-specific check interval.
    6.  `global_polling_interval_seconds`: (Integer) Default check interval if SKU-specific is absent.
    7.  `webdriver_settings`: (Object, optional)
        * `user_agent`: (String, optional) Custom user agent.
    8.  `logging_level`: (String) Logging verbosity ("DEBUG", "INFO", "ERROR").
    9.  `minimum_action_delay_seconds`: (Integer, optional, default: 10) Minimum time in seconds between consecutive browser actions (clicks, typing, page navigation).
    10. `retry_settings`: (Object)
        * `max_retry_attempts`: (Integer) Maximum number of retry attempts for failed operations.
        * `initial_backoff_seconds`: (Integer) Initial delay before first retry.
        * `max_backoff_seconds`: (Integer) Maximum delay between retries.
        * `backoff_multiplier`: (Float) Multiplier for exponential backoff (e.g., 2.0 for doubling).

## II. Core Functionality
### A. SKU Monitoring & Availability Check
    1.  Iterate through `skus_to_monitor`.
    2.  For each SKU, check against `purchase_log.json` (see section VII) to see if `target_quantity` has been met. If met, skip this SKU for purchasing actions but continue monitoring if desired for logging.
    3.  Construct product page URL: `https://www.corsair.com/{region}/en/p/{sku_id}` (or other verified pattern).
    4.  Load product page via Selenium (.NET).
    5.  Identify "Add to Cart" button. Availability based on its `<span>` child element's text:
        * **In Stock:** `<span>` text is "Add to cart".
        * **Out of Stock:** `<span>` text is "Notify Me When In Stock" (or any other differing text).
    6.  If out of stock, log to `stdout`: "Item Unavailable: [SKU Name/ID], [YYYY-MM-DD], [HH:MM:SS]".
    7.  Polling Intervals: Use SKU-specific `polling_interval_seconds` or `global_polling_interval_seconds`. Ensure the time since the *last action across the entire bot* respects `minimum_action_delay_seconds`.
### B. Account Login
    1.  Log into Corsair website using `corsair_account` credentials from `config.json`.
    2.  Perform at program start or before the first purchase attempt.
### C. Default Address Verification
    1.  After login, navigate to `https://www.corsair.com/{region}/en/account/address`.
    2.  Check for `<div>` with title attribute "Default Address".
    3.  Log to `stdout`:
        * If found: "account has default address declared, use of a dummy address in this field is highly encouraged to prevent permanent account suspension"
        * If not found: "account has no default address declared, it is highly encouraged that a dummy default address be added"
### D. Purchase Process (Automated Checkout)
    1.  **Initiate Purchase:** If item "In Stock" AND `target_quantity` for the SKU (from `config.json`) has not yet been met (checked against `purchase_log.json`), proceed.
    2.  **Quantity to Add to Cart:** Determine how many units of the SKU to add to the cart. This should typically be 1 per transaction, unless the site allows adding multiple directly before proceeding to cart (this needs verification on Corsair's site). The bot will attempt multiple transactions if `target_quantity` > 1.
    3.  **Add to Cart:** Click the "Add to Cart" button.
    4.  **View Cart/Sidebar:** Wait for sidebar/mini-cart.
    5.  **Proceed to Checkout:** Click "Checkout" button (identified by a `<span>` with text "Checkout") in sidebar. Navigate to `checkout.corsair.com`.
    6.  **Address Entry on Checkout Page:**
        * Use `shipping_address` from `config.json`.
        * Click "Use a different address" button (per `useDifferentAddressElement.txt`).
        * Fill new address form (per `inputNewAddressElement.txt`) with config data.
        * Save the new address.
    7.  **Payment Information:**
        * Default to "Credit card" (per `paymentFormElement.txt`).
        * Fill credit card form (potentially within `<iframe>`s, per `paymentFormElement.txt`) with `payment_details` from config.
    8.  **Terms and Conditions:**
        * Check "Terms and Conditions" checkbox (per `termsAndConditionsElement.txt`).
    9.  **Finalize Purchase:** Click "Pay now" button (per `paymentFormElement.txt`).
    10. **Order Confirmation & Logging:**
        * Upon successful purchase confirmation (e.g., order number displayed, confirmation page URL), update `purchase_log.json` for the SKU, incrementing the `purchased_quantity`.
        * Log successful order.

## III. CAPTCHA and Anti-Bot Measures
### A. CAPTCHA Handling
    * No automated CAPTCHA solving.
    * If CAPTCHA detected, halt current purchase attempt and log.
### B. Rate Limiting & Delays
    * Enforce `minimum_action_delay_seconds` (default 10 seconds) between any browser action taken by the bot. This is a global delay, not per SKU.
    * Polling intervals for availability checks should generally be longer (minutes).

## IV. Logging and Error Handling
### A. Logging
    * **Levels:** "DEBUG", "INFO", "ERROR" (configurable).
    * **Output:** `stdout` / `stderr`.
    * **Content:**
        * `DEBUG`: All significant actions, navigations, interactions.
        * `INFO`: Key events (availability, purchase initiation/status, login, address check, quantity tracking).
        * `ERROR`: All errors and exceptions.
### B. Error Handling
    1.  **Unexpected State/Page/Element Not Found:**
        * Log error.
        * Dump current page HTML to `debugSnapshots/debugPageSnapshot.html` (within `/data` mount).
        * Stop current purchase attempt; continue monitoring/next SKU.
    2.  **Network Errors:** Handle with retries (for availability checks) or log and move on (purchase steps).

## V. Containerization (Docker)
### A. Technology Stack
    * **Language/Framework:** .NET 9
    * **Browser Automation:** Selenium WebDriver for .NET.
### B. Browser Configuration
    1.  **Browser Selection:**
        * Chrome browser with ChromeDriver
        * Headless mode enabled
        * Viewport size: 1024x768
    2.  **Session Management:**
        * Automatic session refresh on timeout/expiration
        * Automatic re-login using configured credentials
        * Cookie persistence handled by WebDriver's default behavior
    3.  **Error Recovery:**
        * Configurable maximum retry attempts in `config.json`
        * Exponential backoff algorithm for retries
        * Debug snapshots saved for failed purchase attempts
        * Browser crash recovery via container restart
        * Stale element handling via WebDriver's built-in wait mechanisms
    4.  **Concurrency:**
        * Strictly sequential operations
        * Minimum 10-second delay between actions
        * No parallel processing to avoid anti-bot detection
    5.  **Resource Management:**
        * Memory limits configured via Docker container
        * Browser memory management via Chrome's built-in mechanisms
        * Temporary file cleanup on container restart
    6.  **Network Configuration:**
        * Proxy support via WebDriver configuration
        * WireGuard tunnel support via separate configuration file
        * SSL/TLS enabled by default
        * Indefinite retry on network failures with error logging
    7.  **Data Validation:**
        * Input validation for known configuration fields
        * No validation for SKU IDs or shipping addresses
        * Configuration file format validation
    8.  **Monitoring and Health:**
        * Health check endpoint exposed by container
        * Monitoring via stdout/stderr
        * Logging handled by container host
        * Loop detection via action timestamp tracking
    9.  **Security:**
        * Support for secrets management systems
        * Direct secret injection allowed
        * SSL/TLS certificate handling
    10. **Testing:**
        * Test cases for critical purchase flow
        * Mock data for testing
        * Test environment configuration
    11. **Updates:**
        * Version-based container updates
        * Tag-based versioning
    12. **Website Changes:**
        * Automatic detection of critical element changes
        * Error reporting for unrecognized page states
        * Debug snapshots for failed element detection

### C. Mount Points
    1.  `/config`: For `config.json` and `wireguard.json`.
    2.  `/data`: For `purchase_log.json` and `debugSnapshots`.

### D. Additional Configuration (`wireguard.json`)
* **Type:** JSON
* **Location:** Mounted into Docker container via a `/config` volume.
* **Content:**
    1.  Standard WireGuard client configuration
    2.  Interface and peer settings
    3.  Key management

### E. Health Check Endpoint
* **Port:** 8080 (configurable)
* **Endpoint:** `/health`
* **Response:** JSON with status and metrics
* **Metrics:**
    1.  Uptime
    2.  Last successful action
    3.  Current monitoring status
    4.  Error counts
    5.  Purchase attempt history

## VI. Ethical Considerations and User Warnings
### A. In-Program Warnings (Log to `stdout` on start)
    * "Use this program at your own risk."
    * "Automated purchasing may be against Corsair's Terms of Service..."
    * "Consider using a VPN or other IP obfuscation methods."
    * "It is highly recommended to use a temporary or burner Corsair account and a virtual credit card if possible."
    * "Ensure compliance with all applicable laws and website terms."
### B. `robots.txt`
    * Ignore `robots.txt`.

## VII. Purchase Tracking
### A. Purchase Log File (`purchase_log.json`)
* **Purpose:** To keep track of successfully purchased quantities for each SKU to avoid exceeding the `target_quantity`.
* **Location:** Stored in the `/data` volume mount to persist across container restarts.
* **Format:** A JSON object where keys are `sku_id`s.
    ```json
    {
      "SKU123": {
        "target_quantity": 5, // Mirrored from config for reference
        "purchased_quantity": 2,
        "last_purchase_timestamp": "YYYY-MM-DDTHH:MM:SSZ"
      },
      "SKU789": {
        "target_quantity": 1,
        "purchased_quantity": 1,
        "last_purchase_timestamp": "YYYY-MM-DDTHH:MM:SSZ"
      }
      // ... more SKUs
    }
    ```
* **Logic:**
    1.  On program start, load `purchase_log.json`. If it doesn't exist, create an empty one or initialize from `skus_to_monitor` in `config.json` with `purchased_quantity: 0`.
    2.  Before attempting a purchase for an SKU, consult this log. If `purchased_quantity` >= `target_quantity` from `config.json`, do not attempt purchase for that SKU.
    3.  After a confirmed successful purchase, update the `purchased_quantity` for the SKU in this log and save the file.
    4.  If `target_quantity` in `config.json` is changed for an SKU that already has entries in `purchase_log.json`, the bot should respect the new `target_quantity`.