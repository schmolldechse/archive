# Voldechse Archive Bot identity worker

This Cloudflare Worker serves the public bot identity and the signed Web Bot Auth key directory for `archive.voldechse.wtf`.

Required deployment values:

- Secret `WEB_BOT_AUTH_PRIVATE_KEY_PKCS8`: base64-encoded DER PKCS#8 Ed25519 private key.
- Variable `WEB_BOT_AUTH_PUBLIC_KEY_X`: base64url-encoded raw 32-byte Ed25519 public key.

The private key must never be placed in `wrangler.jsonc` or committed.
