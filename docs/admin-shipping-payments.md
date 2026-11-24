# Admin, shipping and payment design

The standalone local demo now groups Admin into four tabs:

1. Orders Status — review orders and update status.
2. Product Addition — add products and see current stock, weight and shipping charge.
3. Courier Configuration — configure existing or custom courier providers and WhatsApp placeholders.
4. Payment Configurations — enable COD, Easypaisa, JazzCash, bank transfer, card gateway placeholder, or custom methods; add payment instructions and QR images.

## Shipping calculation used by the local demo

- Sum the weight of all cart lines (unit weight × quantity).
- Apply only one product shipping charge per order: the highest shipping charge among selected products. If none is set, use the configured base shipping charge.
- Included weight defaults to 5 kg.
- For weight above the included limit, add `ceil(total weight - included weight) × extra charge per kg`.
- Display subtotal, one shipping charge and grand total at checkout.

This is a configurable starter rule, not a courier's live rate quote. Confirm actual city/zone, volumetric-weight and COD fees with the chosen courier before production. Payment QR is a manual transfer instruction; online payment confirmation must be reconciled or integrated with a payment gateway. Never store live API secrets in browser localStorage.
