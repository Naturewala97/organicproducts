# Quick local preview

Open `RUN-LOCAL.html` directly in Chrome, Edge or Firefox. No install is needed. The standalone preview includes:

- Storefront, cart and checkout
- Admin tabs: Orders Status, Product Addition, Courier Configuration, Payment Configurations
- Product stock visibility, product weight and per-product shipping charge
- One shipping charge per order: highest selected product shipping charge is applied once; weight above the configured included limit adds the configured per-kg amount
- Courier provider settings for PostEx, Leopards, TCS, M&P and custom providers
- WhatsApp Business configuration placeholders
- COD, Easypaisa, JazzCash, Bank Transfer, Card Payment and custom online payment options
- QR code URL or image upload for manual online payment instructions
- Order status updates and JSON export

Demo data and settings are stored in this browser's localStorage. This is a local UI prototype only: it does not send live courier bookings or WhatsApp messages, verify online payments, or provide secure multi-user administration. API credentials must be integrated server-side before production use. Card payments require a real payment gateway; displaying a QR code is a manual transfer flow, not card processing.

For the separate full source stack, follow `README.md` and run `docker compose up --build`. The React/API starter has not yet been wired to all of the local demo's new admin settings; those require database models and backend integrations before production deployment.
