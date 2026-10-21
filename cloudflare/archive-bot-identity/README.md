# Voldechse Archive Bot identity worker

This Cloudflare Worker publishes the identity used by the Archive capture worker and its Web Bot Auth key directory.

## Routes

| Path | Response |
| --- | --- |
| `/` | Human-readable bot identity, purpose, user agent, and crawl behavior. |
| `/robots.txt` | Allows indexing of the identity origin. |
| `/.well-known/http-message-signatures-directory` | Signed Web Bot Auth key directory. |

Other paths return `404`. The three routes accept `GET` and `HEAD`; other methods return `405`.

## Install and check

```powershell
bun install --frozen-lockfile
bun run check
```

`bun run check` runs `wrangler deploy --dry-run`. It bundles and validates the worker without publishing it.

## Key material

The worker uses one Ed25519 key pair in two forms:

| Binding | Wrangler type | Format |
| --- | --- | --- |
| `WEB_BOT_AUTH_PRIVATE_KEY_PKCS8` | Secret | Base64-encoded DER PKCS#8 private key |
| `WEB_BOT_AUTH_PUBLIC_KEY_X` | Plain variable | Unpadded base64url raw 32-byte public key (`x` from an OKP JWK) |

Generate a PEM key with OpenSSL:

```powershell
openssl genpkey -algorithm Ed25519 -out web-bot-auth-private-key.pem
```

Derive the private value expected by Wrangler:

```powershell
openssl pkey -in web-bot-auth-private-key.pem -outform DER | openssl base64 -A
```

Store that output through Wrangler:

```powershell
bunx wrangler secret put WEB_BOT_AUTH_PRIVATE_KEY_PKCS8
```

Derive the public `x` value from the same PEM file with Bun's Node.js-compatible crypto API:

```powershell
bun -e "const fs=require('node:fs'),c=require('node:crypto'); const k=c.createPrivateKey(fs.readFileSync('web-bot-auth-private-key.pem')); console.log(c.createPublicKey(k).export({format:'jwk'}).x)"
```

Set that value as `WEB_BOT_AUTH_PUBLIC_KEY_X` in `wrangler.jsonc`. It is public key material and is returned in the directory response.

The .NET capture worker reads the PEM form of the same private key. Its `Archive:WebBotAuth:PrivateKeyPath` points to that file, while `Archive:WebBotAuth:SignatureAgent` identifies this Worker's HTTPS origin.

## Domain

`wrangler.jsonc` configures a custom domain. When changing it, update all three places together:

1. the Worker's custom-domain route;
2. `Archive:WebBotAuth:SignatureAgent` or `ARCHIVE_WEB_BOT_AUTH_SIGNATURE_AGENT` in the capture worker;
3. the user-agent information URL if the public bot URL changes.

The signature agent is an origin such as `https://bot.archive.example.com`, without a path. Verifiers discover its directory at:

```text
https://bot.archive.example.com/.well-known/http-message-signatures-directory
```

## Deploy

Authenticate Wrangler for the intended Cloudflare account, confirm the custom domain and public key in `wrangler.jsonc`, then run:

```powershell
bun run deploy
```

Deployment uses the package's pinned Wrangler version through the package script.

## Verify

Request all public resources from the deployed origin:

```powershell
Invoke-WebRequest https://bot.archive.example.com/
Invoke-WebRequest https://bot.archive.example.com/robots.txt
Invoke-WebRequest https://bot.archive.example.com/.well-known/http-message-signatures-directory
```

For the directory response, verify:

- HTTP status `200`;
- content type `application/http-message-signatures-directory+json`;
- `Signature-Input` and `Signature` headers;
- one `OKP`/`Ed25519` key whose `x` equals the value derived from the deployed private key;
- a short public cache lifetime.

The response is generated with a fresh creation time, expiry, and nonce. The key ID is the JWK thumbprint of the public key.

## Related configuration

- [Archive configuration](../../docs/configuration.md#web-bot-auth)
- [Production deployment](../../docs/deployment.md#web-bot-auth)
