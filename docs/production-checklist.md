# Production launch checklist

- [ ] Choose and register your brand domain.
- [ ] Replace Northstar demo branding, copy, products and images.
- [ ] Use a strong unique database password and admin secret.
- [ ] Replace demo API-key admin access with proper identity and roles.
- [ ] Add request validation, rate limiting, CSRF strategy where applicable, security headers and audit logging.
- [ ] Add database migrations and test restore from backups.
- [ ] Add payment gateway integration and signed webhook verification.
- [ ] Add shipping provider, tracking, email/SMS and returns/refunds workflows.
- [ ] Add privacy policy, terms, shipping/return policy and contact information.
- [ ] Configure HTTPS, DNS, firewall, monitoring and alerting.
- [ ] Test concurrency for stock reservation, duplicate submissions and payment/order reconciliation.
- [ ] Perform security review and load testing before launch.


## Courier and customer messaging
- [ ] Obtain PostEx merchant API documentation, API token, and pickup address code.
- [ ] Implement and test the PostEx shipment-create, cancel, and tracking adapters server-side.
- [ ] Obtain Meta WhatsApp Business Cloud API access and approval for an order-confirmation template.
- [ ] Add retry queue, idempotency keys, webhook verification, and delivery logs before production.
- [ ] Add and test additional courier adapters (Leopards, TCS, M&P) independently.
- [ ] Never expose courier or WhatsApp access tokens in frontend code or browser storage.
