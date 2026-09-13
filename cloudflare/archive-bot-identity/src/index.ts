interface Env {
  WEB_BOT_AUTH_PRIVATE_KEY_PKCS8: string;
  WEB_BOT_AUTH_PUBLIC_KEY_X: string;
}

const DIRECTORY_PATH = "/.well-known/http-message-signatures-directory";
const DIRECTORY_CONTENT_TYPE = "application/http-message-signatures-directory+json";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response("Method Not Allowed", { status: 405, headers: { Allow: "GET, HEAD" } });
    }

    const url = new URL(request.url);
    if (url.pathname === DIRECTORY_PATH) {
      return directoryResponse(request, env, request.method === "HEAD");
    }
    if (url.pathname === "/" || url.pathname === "/robots.txt") {
      return url.pathname === "/robots.txt" ? robotsResponse(request.method === "HEAD") : identityResponse(request.method === "HEAD");
    }
    return new Response("Not Found", { status: 404 });
  }
} satisfies ExportedHandler<Env>;

async function directoryResponse(request: Request, env: Env, head: boolean): Promise<Response> {
  const publicKeyX = requireBase64Url(env.WEB_BOT_AUTH_PUBLIC_KEY_X, "WEB_BOT_AUTH_PUBLIC_KEY_X");
  const keyId = await jwkThumbprint(publicKeyX);
  const created = Math.floor(Date.now() / 1000);
  const expires = created + 300;
  const nonce = toBase64(crypto.getRandomValues(new Uint8Array(32)));
  const signatureParams = `(\"@authority\";req);created=${created};expires=${expires};keyid=\"${keyId}\";alg=\"ed25519\";nonce=\"${nonce}\";tag=\"http-message-signatures-directory\"`;
  const authority = new URL(request.url).host;
  const signatureBase = `\"@authority\";req: ${authority}\n\"@signature-params\": ${signatureParams}`;
  const privateKey = await importPrivateKey(env.WEB_BOT_AUTH_PRIVATE_KEY_PKCS8);
  const signature = await crypto.subtle.sign("Ed25519", privateKey, new TextEncoder().encode(signatureBase));
  const body = JSON.stringify({ keys: [{ kty: "OKP", crv: "Ed25519", x: publicKeyX }] });
  const headers = new Headers({
    "Content-Type": DIRECTORY_CONTENT_TYPE,
    "Cache-Control": "public, max-age=300",
    "Signature-Input": `sig1=${signatureParams}`,
    "Signature": `sig1=:${toBase64(new Uint8Array(signature))}:`,
    "X-Content-Type-Options": "nosniff"
  });
  return new Response(head ? null : body, { status: 200, headers });
}

function identityResponse(head: boolean): Response {
  const html = `<!doctype html>
<html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Voldechse Archive Bot</title><style>
body{font:17px/1.6 system-ui,sans-serif;max-width:760px;margin:4rem auto;padding:0 1.25rem;color:#172033;background:#f7f8fb}
main{background:white;padding:2rem;border-radius:14px;box-shadow:0 8px 30px #15204018}code{background:#eef1f7;padding:.15rem .4rem;border-radius:4px}
h1{line-height:1.2}a{color:#175ac6}</style></head><body><main>
<h1>Voldechse Archive Bot</h1>
<p>This directly operated bot creates user-requested archival snapshots of public web pages. It does not authenticate as users, solve CAPTCHAs, evade site-owner controls, or collect content for model training.</p>
<p>Dieser direkt betriebene Bot erstellt auf Benutzeranforderung Archivkopien öffentlich erreichbarer Webseiten. Er meldet sich nicht als Benutzer an, löst keine CAPTCHAs und umgeht keine ausdrücklichen Betreiberregeln.</p>
<dl><dt>User-Agent</dt><dd><code>VoldechseArchiveBot/1.0 (+https://archive.voldechse.wtf/)</code></dd>
<dt>Operator</dt><dd>Voldechse</dd>
<dt>Verification</dt><dd>Cloudflare Web Bot Auth (Ed25519 HTTP Message Signatures)</dd></dl>
<h2>Crawl policy</h2><p>The bot observes <code>robots.txt</code>, <code>crawl-delay</code>, <code>X-Robots-Tag</code>, and matching HTML robots directives. It applies a minimum two-second interval per origin and does not store pages marked <code>noarchive</code> or <code>none</code>.</p>
<p><a href="${DIRECTORY_PATH}">HTTP Message Signatures key directory</a></p>
</main></body></html>`;
  return new Response(head ? null : html, {
    headers: { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "public, max-age=3600", "X-Content-Type-Options": "nosniff" }
  });
}

function robotsResponse(head: boolean): Response {
  return new Response(head ? null : "User-agent: *\nAllow: /\n", {
    headers: { "Content-Type": "text/plain; charset=utf-8", "Cache-Control": "public, max-age=3600" }
  });
}

async function importPrivateKey(encoded: string): Promise<CryptoKey> {
  if (!encoded || encoded === "SET_WITH_WRANGLER_OR_LOCAL_ENV") throw new Error("WEB_BOT_AUTH_PRIVATE_KEY_PKCS8 is not configured");
  const der = Uint8Array.from(atob(encoded.replace(/\s+/g, "")), character => character.charCodeAt(0));
  return crypto.subtle.importKey("pkcs8", der, { name: "Ed25519" }, false, ["sign"]);
}

async function jwkThumbprint(publicKeyX: string): Promise<string> {
  const canonical = new TextEncoder().encode(`{"crv":"Ed25519","kty":"OKP","x":"${publicKeyX}"}`);
  return toBase64Url(new Uint8Array(await crypto.subtle.digest("SHA-256", canonical)));
}

function requireBase64Url(value: string, name: string): string {
  if (!value || value === "SET_WITH_WRANGLER_OR_LOCAL_ENV" || !/^[A-Za-z0-9_-]{43}$/.test(value)) throw new Error(`${name} is not configured correctly`);
  return value;
}

function toBase64(value: Uint8Array): string {
  let binary = "";
  for (const byte of value) binary += String.fromCharCode(byte);
  return btoa(binary);
}

function toBase64Url(value: Uint8Array): string {
  return toBase64(value).replace(/=/g, "").replace(/\+/g, "-").replace(/\//g, "_");
}
