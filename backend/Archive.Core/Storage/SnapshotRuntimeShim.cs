using System.Text.Json;

namespace Archive.Core.Storage;

/// <summary>Builds the browser-side compatibility and network-containment layer for archived documents.</summary>
public static class SnapshotRuntimeShim
{
    /// <summary>Builds the deterministic runtime shim for one archived document and its captured resources.</summary>
    public static string Build(string? sourceUrl, IReadOnlyDictionary<string, string> resources)
    {
        var serializedMap = JsonSerializer.Serialize(resources);
        var serializedBase = JsonSerializer.Serialize(sourceUrl);
        return $$"""
            (() => {
              "use strict";
              const originalBase = {{serializedBase}} || "about:blank";
              const resources = Object.freeze({{serializedMap}});
              const nativeFetch = window.fetch.bind(window);
              const withoutHash = (value, base) => {
                const url = new URL(String(value), base);
                url.hash = "";
                return url.href;
              };
              const sourceByLocal = Object.create(null);
              for (const [source, local] of Object.entries(resources))
                sourceByLocal[withoutHash(local, document.baseURI)] = source;
              const localRoot = withoutHash("./", document.baseURI);
              const tryResolve = (input) => {
                const raw = input instanceof URL ? input.href : String(input);
                const candidate = new URL(raw, document.baseURI);
                // Blob and data URLs are generated from already captured bytes inside the sandbox. They do not
                // create an outbound request and must remain usable by loaders for embedded images and media.
                if (candidate.protocol === "blob:" || candidate.protocol === "data:" || candidate.href === "about:blank")
                  return candidate.href;
                const localUrl = withoutHash(raw, document.baseURI);
                if (sourceByLocal[localUrl]) return localUrl;
                // Libraries may resolve a relative source against the rewritten document before calling fetch.
                // Recover that snapshot-relative suffix and resolve it against the archived document instead.
                const sourceUrl = localUrl.startsWith(localRoot)
                  ? withoutHash(localUrl.slice(localRoot.length), originalBase)
                  : withoutHash(raw, originalBase);
                const local = resources[sourceUrl];
                return local ? withoutHash(local, document.baseURI) : null;
              };
              const resolve = (input) => {
                const local = tryResolve(input);
                if (!local) throw new TypeError("The archived snapshot blocked an uncaptured resource.");
                return local;
              };

              window.fetch = (input, init = {}) => {
                const request = input instanceof Request ? input : null;
                const method = String(init.method || request?.method || "GET").toUpperCase();
                if (method !== "GET" && method !== "HEAD")
                  return Promise.reject(new TypeError("The archived snapshot permits only GET and HEAD."));
                const url = resolve(request?.url || input);
                return nativeFetch(url, { ...init, method, credentials: "omit", redirect: "error", referrerPolicy: "no-referrer" });
              };
              const nativeOpen = XMLHttpRequest.prototype.open;
              XMLHttpRequest.prototype.open = function(method, url, ...rest) {
                const normalized = String(method).toUpperCase();
                if (normalized !== "GET" && normalized !== "HEAD")
                  throw new TypeError("The archived snapshot permits only GET and HEAD.");
                return nativeOpen.call(this, normalized, resolve(url), ...rest);
              };

              // Legacy loaders inspect script.src to discover their original library root. Return the archived
              // source URL to page code while every assignment still resolves to a captured local object.
              const urlProperties = [
                [window.HTMLScriptElement, "src"], [window.HTMLImageElement, "src"],
                [window.HTMLSourceElement, "src"], [window.HTMLIFrameElement, "src"],
                [window.HTMLMediaElement, "src"], [window.HTMLVideoElement, "poster"],
                [window.HTMLLinkElement, "href"], [window.HTMLInputElement, "src"]
              ];
              for (const [constructor, property] of urlProperties) {
                if (!constructor) continue;
                const descriptor = Object.getOwnPropertyDescriptor(constructor.prototype, property);
                if (!descriptor?.get || !descriptor?.set || descriptor.configurable === false) continue;
                try {
                  Object.defineProperty(constructor.prototype, property, {
                    ...descriptor,
                    get() {
                      const local = descriptor.get.call(this);
                      return constructor === window.HTMLScriptElement
                        ? sourceByLocal[withoutHash(local, document.baseURI)] || local
                        : local;
                    },
                    set(value) {
                      if (this instanceof HTMLImageElement || this instanceof HTMLMediaElement)
                        this.crossOrigin = "anonymous";
                      descriptor.set.call(this, resolve(value));
                    }
                  });
                } catch {}
              }

              const networkAttributes = Object.freeze({
                SCRIPT: new Set(["src"]), IMG: new Set(["src"]), SOURCE: new Set(["src"]),
                IFRAME: new Set(["src"]), AUDIO: new Set(["src"]), VIDEO: new Set(["src", "poster"]),
                LINK: new Set(["href"]), INPUT: new Set(["src"])
              });
              const nativeSetAttribute = Element.prototype.setAttribute;
              Element.prototype.setAttribute = function(name, value) {
                const normalized = String(name).toLowerCase();
                const guarded = networkAttributes[this.tagName];
                if (guarded?.has(normalized) && normalized === "src" &&
                    (this instanceof HTMLImageElement || this instanceof HTMLMediaElement))
                  this.crossOrigin = "anonymous";
                return nativeSetAttribute.call(this, name, guarded?.has(normalized) ? resolve(value) : value);
              };

              // Parser-era dependency loaders commonly use document.write. Rewrite complete URL attributes before
              // the browser can request them; partial or uncaptured markup remains subject to the snapshot CSP.
              const rewriteMarkup = (markup) => String(markup).replace(
                /\b(src|href|poster)\s*=\s*(["'])(.*?)\2/gi,
                (match, attribute, quote, value) => {
                  const local = tryResolve(value);
                  return local ? `${attribute}=${quote}${local}${quote}` : match;
                });
              const nativeWrite = Document.prototype.write;
              const nativeWriteln = Document.prototype.writeln;
              Document.prototype.write = function(...parts) {
                return nativeWrite.call(this, ...parts.map(rewriteMarkup));
              };
              Document.prototype.writeln = function(...parts) {
                return nativeWriteln.call(this, ...parts.map(rewriteMarkup));
              };

              for (const name of ["WebSocket", "EventSource", "WebTransport", "Worker", "SharedWorker", "RTCPeerConnection", "webkitRTCPeerConnection"])
                try { Object.defineProperty(window, name, { value: class { constructor() { throw new TypeError("The archived snapshot blocked this capability."); } } }); } catch {}
              window.open = () => null;
              addEventListener("submit", event => event.preventDefault(), true);
            })();
            """;
    }
}
