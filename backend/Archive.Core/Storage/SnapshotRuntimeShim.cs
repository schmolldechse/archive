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
              const blockedMessage = "The archived snapshot blocked this capability.";
              const blockedConstructor = class { constructor() { throw new TypeError(blockedMessage); } };
              const lock = (target, name, value) => {
                try { Object.defineProperty(target, name, { value, writable: false, configurable: false }); }
                catch {}
              };
              const nativeFetch = window.fetch.bind(window);
              const NativeWorker = window.Worker;
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
              const resolveCapturedFrame = (input) => {
                const raw = input instanceof URL ? input.href : String(input);
                const candidate = new URL(raw, document.baseURI);
                if (candidate.protocol === "blob:" || candidate.protocol === "data:" || candidate.href === "about:blank")
                  throw new TypeError("The archived snapshot blocked an uncaptured frame.");
                return resolve(input);
              };

              lock(window, "fetch", (input, init = {}) => {
                const request = input instanceof Request ? input : null;
                const method = String(init.method || request?.method || "GET").toUpperCase();
                if (method !== "GET" && method !== "HEAD")
                  return Promise.reject(new TypeError("The archived snapshot permits only GET and HEAD."));
                const url = resolve(request?.url || input);
                return nativeFetch(url, { ...init, method, credentials: "omit", redirect: "error", referrerPolicy: "no-referrer" });
              });
              const nativeOpen = XMLHttpRequest.prototype.open;
              lock(XMLHttpRequest.prototype, "open", function(method, url, ...rest) {
                const normalized = String(method).toUpperCase();
                if (normalized !== "GET" && normalized !== "HEAD")
                  throw new TypeError("The archived snapshot permits only GET and HEAD.");
                try { this.withCredentials = false; } catch {}
                return nativeOpen.call(this, normalized, resolve(url), ...rest);
              });

              // Legacy loaders inspect script.src to discover their original library root. Return the archived
              // source URL to page code while every assignment still resolves to a captured local object.
              const urlProperties = [
                [window.HTMLScriptElement, "src"], [window.HTMLImageElement, "src"],
                [window.HTMLSourceElement, "src"], [window.HTMLIFrameElement, "src"],
                [window.HTMLMediaElement, "src"], [window.HTMLVideoElement, "poster"],
                [window.HTMLTrackElement, "src"],
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
                      descriptor.set.call(this, constructor === window.HTMLIFrameElement
                        ? resolveCapturedFrame(value)
                        : resolve(value));
                    }
                  });
                } catch {}
              }

              const networkAttributes = Object.freeze({
                SCRIPT: new Set(["src"]), IMG: new Set(["src"]), SOURCE: new Set(["src"]),
                IFRAME: new Set(["src"]), AUDIO: new Set(["src"]), VIDEO: new Set(["src", "poster"]),
                TRACK: new Set(["src"]), LINK: new Set(["href"]), INPUT: new Set(["src"])
              });
              const nativeSetAttribute = Element.prototype.setAttribute;
              lock(Element.prototype, "setAttribute", function(name, value) {
                const normalized = String(name).toLowerCase();
                const guarded = networkAttributes[this.tagName];
                if (guarded?.has(normalized) && normalized === "src" &&
                    (this instanceof HTMLImageElement || this instanceof HTMLMediaElement))
                  this.crossOrigin = "anonymous";
                const resolver = this instanceof HTMLIFrameElement && normalized === "src"
                  ? resolveCapturedFrame
                  : resolve;
                return nativeSetAttribute.call(this, name, guarded?.has(normalized) ? resolver(value) : value);
              });

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
              lock(Document.prototype, "write", function(...parts) {
                return nativeWrite.call(this, ...parts.map(rewriteMarkup));
              });
              lock(Document.prototype, "writeln", function(...parts) {
                return nativeWriteln.call(this, ...parts.map(rewriteMarkup));
              });

              // Preserve original controls. If an archived autoplay-only element cannot start under the user's
              // browser policy, expose native controls as a local replay fallback instead of leaving it inert.
              const enableMediaFallback = () => {
                for (const media of document.querySelectorAll("audio[autoplay]:not([controls]), video[autoplay]:not([controls])")) {
                  const candidates = [media.getAttribute("src"),
                    ...Array.from(media.querySelectorAll("source[src]"), source => source.getAttribute("src"))];
                  if (!candidates.some(candidate => candidate && tryResolve(candidate))) continue;
                  let attempt;
                  try { attempt = media.play(); }
                  catch { media.controls = true; continue; }
                  if (attempt?.catch)
                    attempt.catch(() => { if (media.isConnected) media.controls = true; });
                }
              };
              if (document.readyState === "loading")
                document.addEventListener("DOMContentLoaded", enableMediaFallback, { once: true });
              else
                queueMicrotask(enableMediaFallback);

              const workerResources = Object.freeze(Object.fromEntries(Object.entries(resources)
                .map(([source, local]) => [source, withoutHash(local, document.baseURI)])));
              const buildWorkerBootstrap = (target, source, type) => {
                const serializedResources = JSON.stringify(workerResources);
                const serializedTarget = JSON.stringify(target);
                const serializedSource = JSON.stringify(source);
                const loadEntry = type === "module"
                  ? `const pendingMessages = [];
                     const queueMessage = event => {
                       event.stopImmediatePropagation();
                       pendingMessages.push(event);
                     };
                     self.addEventListener("message", queueMessage);
                     import(${serializedTarget}).then(() => {
                       self.removeEventListener("message", queueMessage);
                       for (const event of pendingMessages)
                         self.dispatchEvent(new MessageEvent("message", {
                           data: event.data,
                           origin: event.origin,
                           lastEventId: event.lastEventId,
                           ports: event.ports
                         }));
                     }).catch(error => setTimeout(() => { throw error; }));`
                  : `nativeImportScripts(${serializedTarget});`;
                return `(() => {
                  "use strict";
                  const originalBase = ${serializedSource};
                  const entryUrl = ${serializedTarget};
                  const resources = Object.freeze(${serializedResources});
                  const blockedMessage = "The archived snapshot blocked this capability.";
                  const blockedConstructor = class { constructor() { throw new TypeError(blockedMessage); } };
                  const lock = (target, name, value) => {
                    try { Object.defineProperty(target, name, { value, writable: false, configurable: false }); }
                    catch {}
                  };
                  const withoutHash = (value, base) => {
                    const url = new URL(String(value), base);
                    url.hash = "";
                    return url.href;
                  };
                  const sourceByLocal = Object.create(null);
                  for (const [source, local] of Object.entries(resources))
                    sourceByLocal[withoutHash(local, entryUrl)] = source;
                  const tryResolve = (input) => {
                    const raw = input instanceof URL ? input.href : String(input);
                    const candidate = new URL(raw, entryUrl);
                    if (candidate.protocol === "blob:" || candidate.protocol === "data:" || candidate.href === "about:blank")
                      return candidate.href;
                    const localUrl = withoutHash(raw, entryUrl);
                    if (sourceByLocal[localUrl]) return localUrl;
                    const sourceUrl = withoutHash(raw, originalBase);
                    const local = resources[sourceUrl];
                    return local ? withoutHash(local, entryUrl) : null;
                  };
                  const resolve = (input) => {
                    const local = tryResolve(input);
                    if (!local) throw new TypeError("The archived snapshot blocked an uncaptured resource.");
                    return local;
                  };
                  const nativeFetch = self.fetch.bind(self);
                  lock(self, "fetch", (input, init = {}) => {
                    const request = input instanceof Request ? input : null;
                    const method = String(init.method || request?.method || "GET").toUpperCase();
                    if (method !== "GET" && method !== "HEAD")
                      return Promise.reject(new TypeError("The archived snapshot permits only GET and HEAD."));
                    return nativeFetch(resolve(request?.url || input), {
                      ...init, method, credentials: "omit", redirect: "error", referrerPolicy: "no-referrer"
                    });
                  });
                  if (typeof XMLHttpRequest !== "undefined") {
                    const nativeOpen = XMLHttpRequest.prototype.open;
                    lock(XMLHttpRequest.prototype, "open", function(method, url, ...rest) {
                      const normalized = String(method).toUpperCase();
                      if (normalized !== "GET" && normalized !== "HEAD")
                        throw new TypeError("The archived snapshot permits only GET and HEAD.");
                      try { this.withCredentials = false; } catch {}
                      return nativeOpen.call(this, normalized, resolve(url), ...rest);
                    });
                  }
                  const nativeImportScripts = self.importScripts?.bind(self);
                  if (nativeImportScripts)
                    lock(self, "importScripts", (...urls) => nativeImportScripts(...urls.map(resolve)));
                  const NativeWorker = self.Worker;
                  if (NativeWorker) {
                    const workerResources = resources;
                    const buildWorkerBootstrap = ${buildWorkerBootstrap.toString()};
                    const ArchivedWorker = new Proxy(NativeWorker, {
                      construct(target, args) {
                        const raw = args[0] instanceof URL ? args[0].href : String(args[0]);
                        const candidate = new URL(raw, entryUrl);
                        const options = args[1] && typeof args[1] === "object"
                          ? { ...args[1], credentials: "omit" }
                          : { credentials: "omit" };
                        if (candidate.protocol === "blob:")
                          throw new TypeError("The archived snapshot blocked an arbitrary Blob worker.");
                        const local = tryResolve(args[0]);
                        const localUrl = local ? withoutHash(local, entryUrl) : null;
                        const nestedSource = localUrl ? sourceByLocal[localUrl] : null;
                        if (!localUrl || !nestedSource)
                          throw new TypeError("The archived snapshot blocked an uncaptured resource.");
                        const nestedType = options.type === "module" ? "module" : "classic";
                        const bootstrap = new Blob([
                          buildWorkerBootstrap(localUrl, nestedSource, nestedType)
                        ], { type: "text/javascript" });
                        const bootstrapUrl = URL.createObjectURL(bootstrap);
                        try {
                          const bootstrapOptions = nestedType === "module"
                            ? { ...options, type: "classic" }
                            : options;
                          const worker = new target(bootstrapUrl, bootstrapOptions);
                          setTimeout(() => URL.revokeObjectURL(bootstrapUrl));
                          return worker;
                        } catch (error) {
                          URL.revokeObjectURL(bootstrapUrl);
                          throw error;
                        }
                      }
                    });
                    lock(self, "Worker", ArchivedWorker);
                  }
                  for (const name of ["WebSocket", "EventSource", "WebTransport", "SharedWorker", "RTCPeerConnection", "webkitRTCPeerConnection"])
                    lock(self, name, blockedConstructor);
                  ${loadEntry}
                })();`;
              };
              if (NativeWorker) {
                const ArchivedWorker = new Proxy(NativeWorker, {
                  construct(target, args) {
                    const raw = args[0] instanceof URL ? args[0].href : String(args[0]);
                    const candidate = new URL(raw, document.baseURI);
                    const options = args[1] && typeof args[1] === "object"
                      ? { ...args[1], credentials: "omit" }
                      : { credentials: "omit" };
                    if (candidate.protocol === "blob:")
                      throw new TypeError("The archived snapshot blocked an arbitrary Blob worker.");
                    const local = tryResolve(args[0]);
                    const localUrl = local ? withoutHash(local, document.baseURI) : null;
                    const source = localUrl ? sourceByLocal[localUrl] : null;
                    if (!localUrl || !source)
                      throw new TypeError("The archived snapshot blocked an uncaptured resource.");
                    const type = options.type === "module" ? "module" : "classic";
                    const bootstrap = new Blob([buildWorkerBootstrap(localUrl, source, type)],
                      { type: "text/javascript" });
                    const bootstrapUrl = URL.createObjectURL(bootstrap);
                    try {
                      // The bootstrap itself is classic so it can start from an opaque sandbox origin.
                      // A requested module entry still executes as a module through dynamic import().
                      const bootstrapOptions = type === "module" ? { ...options, type: "classic" } : options;
                      const worker = new target(bootstrapUrl, bootstrapOptions);
                      // Module workers may defer their initial Blob read until after construction returns.
                      // Revoke in the next task, after Chromium has retained the entry response.
                      setTimeout(() => URL.revokeObjectURL(bootstrapUrl));
                      return worker;
                    } catch (error) {
                      URL.revokeObjectURL(bootstrapUrl);
                      throw error;
                    }
                  }
                });
                lock(window, "Worker", ArchivedWorker);
              }
              try {
                const serviceWorker = navigator.serviceWorker;
                if (serviceWorker)
                  lock(serviceWorker, "register", () => Promise.reject(new TypeError(blockedMessage)));
              } catch {}
              for (const name of ["WebSocket", "EventSource", "WebTransport", "SharedWorker", "RTCPeerConnection", "webkitRTCPeerConnection"])
                lock(window, name, blockedConstructor);
              if (typeof Navigator !== "undefined")
                lock(Navigator.prototype, "sendBeacon", () => false);
              lock(window, "open", () => null);
              if (typeof HTMLFormElement !== "undefined") {
                const blockFormSubmission = () => { throw new TypeError(blockedMessage); };
                lock(HTMLFormElement.prototype, "submit", blockFormSubmission);
                lock(HTMLFormElement.prototype, "requestSubmit", blockFormSubmission);
              }
              addEventListener("submit", event => event.preventDefault(), true);
            })();
            """;
    }
}
