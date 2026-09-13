# Cloudflare and Web Bot Auth

The archive worker can identify itself with Cloudflare Web Bot Auth. Each outgoing HTTP request receives a short-lived Ed25519 HTTP Message Signature. The public identity and signed key directory are served by the optional worker in `cloudflare/archive-bot-identity/`.

Web Bot Auth is identity attestation, not a CAPTCHA bypass. It lets a site or intermediary verify which operator controls a request. It does not require a target site to grant access, and it does not override `robots.txt`, `noarchive`, authentication, authorization, or other site-owner policy.

## Public identity endpoints

The configured identity origin serves:

- `/` — human-readable bot purpose, operator, User-Agent, and crawl policy;
- `/robots.txt` — policy for the identity origin itself;
- `/.well-known/http-message-signatures-directory` — the signed public Ed25519 key directory.

The directory response uses:

- media type `application/http-message-signatures-directory+json`;
- `Signature-Input` and `Signature` response headers;
- a JWK thumbprint derived from the public Ed25519 key;
- a fresh creation time, expiration, and nonce.

The deployed identity URL is currently `https://archive.voldechse.wtf/`. Change the worker configuration and archive User-Agent together if that origin changes.

## Key material

Two deployments need matching key material:

1. The archive worker reads the private key from a read-only local or orchestrator mount.
2. The Cloudflare identity worker receives the private key as the `WEB_BOT_AUTH_PRIVATE_KEY_PKCS8` secret and the raw public key as `WEB_BOT_AUTH_PUBLIC_KEY_X`.

Never place the private key in:

- Git;
- `wrangler.jsonc`;
- an example configuration;
- a Docker build context;
- a Docker image layer;
- logs or command output.

The local repository path expected by Compose is:

```text
.secrets/web-bot-auth-private-key.pem
```

The containing directory is mounted read-only at `/run/secrets`. The worker reads:

```text
/run/secrets/web-bot-auth-private-key.pem
```

On Unix-like hosts, restrict the file to its owner:

```sh
chmod 600 .secrets/web-bot-auth-private-key.pem
stat -c '%a %n' .secrets/web-bot-auth-private-key.pem
```

Do not print the key to verify it.

## Local configuration

Copy `.env.example` to `.env` and set:

```dotenv
ARCHIVE_BROWSER_HEADLESS=false
ARCHIVE_BROWSER_USER_AGENT=VoldechseArchiveBot/1.0 (+https://archive.voldechse.wtf/)
ARCHIVE_WEB_BOT_AUTH_ENABLED=true
ARCHIVE_WEB_BOT_AUTH_SIGNATURE_AGENT=https://archive.voldechse.wtf
ARCHIVE_WEB_BOT_AUTH_KEY_DIRECTORY=./.secrets
ARCHIVE_WEB_BOT_AUTH_PRIVATE_KEY_PATH=/run/secrets/web-bot-auth-private-key.pem
ARCHIVE_WEB_BOT_AUTH_SIGNATURE_LIFETIME_SECONDS=60
ARCHIVE_CRAWL_POLICY_ENABLED=true
ARCHIVE_CRAWL_USER_AGENT_TOKEN=VoldechseArchiveBot
ARCHIVE_MINIMUM_HOST_DELAY_SECONDS=2
```

If the key lives elsewhere on the host, set `ARCHIVE_WEB_BOT_AUTH_KEY_DIRECTORY` to its containing directory. The filename inside that directory must still match `ARCHIVE_WEB_BOT_AUTH_PRIVATE_KEY_PATH`.

Validate and start the stack:

```sh
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

## Identity worker configuration

The Cloudflare project is in `cloudflare/archive-bot-identity/`.

Install dependencies:

```sh
cd cloudflare/archive-bot-identity
npm ci
```

Configure the private key as a Wrangler secret:

```sh
npx wrangler secret put WEB_BOT_AUTH_PRIVATE_KEY_PKCS8
```

Configure `WEB_BOT_AUTH_PUBLIC_KEY_X` as a non-secret worker variable. It is the base64url-encoded raw 32-byte Ed25519 public key and is intentionally public. Keep the public value and private key from the same key pair.

Check the worker package without publishing:

```sh
npm run check
```

Deploy only after reviewing the configured account, route, and public key:

```sh
npm run deploy
```

Wrangler local state and logs under `.wrangler/` and local `.dev.vars` files are ignored.

## Verify the public identity

```sh
curl -i https://archive.voldechse.wtf/
curl -i https://archive.voldechse.wtf/.well-known/http-message-signatures-directory
```

The key-directory request should return HTTP `200`, the dedicated content type, and both signature headers.

The public key itself is not confidential. Verify that it corresponds to the private key used by the archive worker before requesting external recognition.

## Cloudflare bot recognition

Technical Web Bot Auth support does not automatically add a bot to Cloudflare's verified directory. The operator must use Cloudflare's current bot-submission process and provide:

- a stable bot name;
- the public documentation and identity URL;
- the operator identity;
- the access and content-use categories;
- Web Bot Auth as the identity mechanism;
- the signed key-directory URL.

Submission identifiers, reviewer correspondence, account identifiers, and current review status are operational data and should not be committed to this repository.

Cloudflare may return:

- HTTP `400` for malformed signatures or unsupported signature parameters;
- HTTP `401` for a well-formed but unknown key, or for a signature that does not validate;
- HTTP `200` after a recognized identity is accepted by the relevant verification endpoint.

Target zones remain free to challenge or block a recognized bot.

## Test a capture

Use a neutral public page that permits archiving:

```sh
curl -X POST http://localhost:5200/api/archive \
  -H "Content-Type: application/json" \
  -d '{"sourceUrl":"https://example.com/","title":"Web Bot Auth capture check","tags":["verification"]}'
```

Read the returned job ID:

```sh
curl http://localhost:5200/api/archive/ARCHIVE_ID
docker compose logs --tail=150 worker
```

A successful job must contain the target document, not a challenge or interstitial page. The capture engine explicitly rejects recognized challenge responses and documents.

## Request-signing behavior

When enabled, the archive worker:

- loads an Ed25519 PKCS#8 key during startup and fails closed if it cannot be read;
- derives the raw public key and canonical JWK thumbprint;
- signs `@authority` and `signature-agent`;
- creates a fresh random nonce for every request;
- uses the configured short lifetime;
- adds `Signature-Agent`, `Signature-Input`, and `Signature`;
- signs `robots.txt` requests and browser-routed HTTP requests.

The signing key is loaded into memory only by the worker process. Raw key bytes used during import are cleared after key construction. Signatures and public key identifiers are not secret, but logs should still avoid dumping full request headers unnecessarily.

## Challenge handling

The worker recognizes Cloudflare through:

- the `cf-mitigated: challenge` response header;
- known `/cdn-cgi/challenge-platform/` resources;
- the `challenges.cloudflare.com` host;
- a small set of document-level challenge indicators.

It first establishes access on a disposable page. If access becomes available, it opens a new page in the same browser context for clean capture. Cookies survive, but access-phase resource events do not enter the snapshot collector.

The worker never:

- solves a CAPTCHA;
- asks a user to interact with the worker's browser;
- publishes a recognized challenge page;
- treats a same-origin challenge redirect as permission to archive a different page.

## Production safeguards

For server operation:

- use `Production` for both .NET environments;
- inject the private key through a secret mount;
- keep PostgreSQL and S3-compatible storage off the public Internet;
- restrict worker egress to public HTTP(S) at the firewall or container-network layer;
- monitor key-directory availability and signature validation;
- apply authentication and quotas to the archive-submission API;
- run the browser as an unprivileged user with the required seccomp policy before processing high-risk untrusted targets;
- review Cloudflare and target-site policy changes periodically.

The application-level public-network guard cannot fully prevent DNS rebinding or misconfigured proxies. Network-layer enforcement is mandatory for a hardened deployment.

## Key rotation

Rotate the key as one coordinated operation:

1. Generate a new Ed25519 pair outside the repository.
2. Add the new private key to the identity worker's secret store.
3. Publish the new public key in the signed directory.
4. Obtain any required external recognition for the new key.
5. Update the archive worker's mounted private key.
6. Confirm signed requests with the new key.
7. Remove the old public key only after all consumers have transitioned.

If the directory implementation supports multiple keys in the future, publish old and new public keys during the overlap. The current implementation publishes one key and therefore requires carefully coordinated replacement.

## Troubleshooting

- **The worker cannot find the key:** verify the host directory, filename, read permissions, mount, and configured container path.
- **The worker stops at startup:** check that the private key is valid PKCS#8 Ed25519 material and that Web Bot Auth was not enabled accidentally.
- **The key directory returns `500`:** verify both Cloudflare environment values and confirm that the public key is 43 base64url characters.
- **A verification endpoint returns `400`:** inspect signature components, time, authority, signature-agent, nonce, and encoding.
- **A verification endpoint returns `401`:** confirm that the public and private keys match and check external recognition status.
- **A target still returns a challenge:** recognition may not be active for that zone, or the zone may reject the bot category.
- **A capture fails after a challenge:** the clean page may have encountered a second challenge or an unexpected navigation.

## Official references

- [Cloudflare Web Bot Auth](https://developers.cloudflare.com/bots/reference/bot-verification/web-bot-auth/)
- [Cloudflare Verified Bots](https://developers.cloudflare.com/bots/concepts/bot/verified-bots/)
- [Detect a Cloudflare challenge response](https://developers.cloudflare.com/cloudflare-challenges/challenge-types/challenge-pages/detect-response/)
- [Playwright .NET in Docker](https://playwright.dev/dotnet/docs/docker)
