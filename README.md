# Independent E-Commerce Starter

A self-hosted starter store built with React + Vite, ASP.NET Core 8 Minimal API, and PostgreSQL.
It is a foundation/MVP, not a production-certified payment system.

## Included
- Customer storefront with product search, category filtering, cart and checkout form
- Admin product CRUD and order status management
- ASP.NET Core API with EF Core + PostgreSQL
- Docker Compose for PostgreSQL, API and frontend
- Seed products for local demonstration

## Run locally with Docker
1. Install Docker Desktop / Docker Engine.
2. Copy `.env.example` to `.env` and change all secrets.
3. Run: `docker compose up --build`
4. Storefront: http://localhost:5173
5. API health: http://localhost:8080/health

The admin demo key is configured through `ADMIN_API_KEY`. Send it as the `X-Admin-Key` header for admin endpoints. Change it before any shared deployment.

## Run without Docker
API:
```bash
cd backend
dotnet restore
dotnet run
```
Set `ConnectionStrings__Default` and `Admin__ApiKey` environment variables.

Frontend:
```bash
cd frontend
npm install
npm run dev
```
Set `VITE_API_URL=http://localhost:8080`.

## Important before going live
- Replace demo admin-key authentication with proper user accounts, hashed passwords, MFA, role-based authorization and secure sessions.
- Integrate a payment provider using server-side payment intents and signed webhook verification. Never trust a payment success message from the browser.
- Add shipping-provider integration, email/SMS notifications, tax rules and refund workflows.
- Configure HTTPS, production secrets, database backups, monitoring, rate limiting, audit logs and privacy/terms pages.
- Review local legal, tax and consumer-protection requirements.
- Do not store card numbers or CVV in this application.


## Courier and WhatsApp configuration placeholders

The Admin area includes a Shipping & WhatsApp integrations section. The starter reserves configuration slots for PostEx, Leopards, TCS, M&P and WhatsApp Business Cloud API. No live API calls are made until provider adapters are implemented and valid merchant credentials are supplied.

Set credentials only on the server in `.env` (never in frontend code): `POSTEX_API_URL`, `POSTEX_API_TOKEN`, `POSTEX_PICKUP_ADDRESS_CODE`, `WHATSAPP_PHONE_NUMBER_ID`, `WHATSAPP_ACCESS_TOKEN`, and `WHATSAPP_TEMPLATE_NAME`. These values are passed into the API container by Docker Compose. Other courier integrations can follow the same pattern when accounts are available.

Checkout now collects delivery city as well as name, phone and street address. The standalone `RUN-LOCAL.html` is the richer local UI prototype. Its Admin area is split into Orders Status, Product Addition, Courier Configuration, and Payment Configurations tabs. It demonstrates one shipment charge per order, configurable included weight (default 5 kg) plus additional per-kg charges, product weight/shipping fields, COD and manual online methods with QR images, and custom courier/payment entries. These new settings are currently implemented in the standalone browser demo only; the React/API/PostgreSQL stack still needs matching database models, API endpoints, server-side payment/courier adapters and proper authentication before those settings become persistent multi-user production features. The demo stores settings only in browser localStorage and must not be used to store real production API secrets.
