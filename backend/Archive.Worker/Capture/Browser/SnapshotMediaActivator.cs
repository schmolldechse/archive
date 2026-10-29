using Microsoft.Playwright;

namespace Archive.Worker.Capture.Browser;

/// <summary>Triggers finite native media requests without persisting the activator's temporary playback state.</summary>
internal static class SnapshotMediaActivator
{
    private const string ActivateScript = """
        async () => {
          const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
          const temporaryAttributes = new Map();
          const remember = element => {
            if (!temporaryAttributes.has(element))
              temporaryAttributes.set(element, element.hasAttribute("src") ? element.getAttribute("src") : null);
          };

          for (const element of document.querySelectorAll(
            "video:is([data-src], [data-lazy-src]):not([src]), audio:is([data-src], [data-lazy-src]):not([src]), " +
            "video source:is([data-src], [data-lazy-src]):not([src]), audio source:is([data-src], [data-lazy-src]):not([src]), " +
            "video track:is([data-src], [data-lazy-src]):not([src]), audio track:is([data-src], [data-lazy-src]):not([src])")) {
            const candidate = element.getAttribute("data-src") || element.getAttribute("data-lazy-src");
            if (!candidate) continue;
            remember(element);
            element.setAttribute("src", candidate);
          }

          const mediaElements = Array.from(document.querySelectorAll("video, audio"));
          await Promise.allSettled(mediaElements.map(async media => {
            const wasMuted = media.muted;
            const wasPaused = media.paused;
            try {
              media.scrollIntoView({ block: "center", behavior: "instant" });
              media.muted = true;
              media.load();
              const play = media.play();
              if (play?.catch)
                await Promise.race([play.catch(() => undefined), delay(1500)]);
              else
                await delay(100);
            } finally {
              if (wasPaused) media.pause();
              media.muted = wasMuted;
            }
          }));

          for (const [element, original] of temporaryAttributes) {
            if (original === null) element.removeAttribute("src");
            else element.setAttribute("src", original);
          }
        }
        """;

    public static async Task ActivateAsync(IPage page, TimeSpan quietPeriod, CancellationToken cancellationToken)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var frame in page.Frames.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsCapturedFrame(frame.Url))
                    continue;
                try
                {
                    await frame.EvaluateAsync(ActivateScript).WaitAsync(cancellationToken);
                }
                catch (PlaywrightException) when (frame.IsDetached)
                {
                    // A player may remove its own frame while the bounded behavior is running.
                }
            }

            if (pass == 0)
                await Task.Delay(quietPeriod, cancellationToken);
        }
    }

    private static bool IsCapturedFrame(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
