<script lang="ts">
	import BrandMark from "./BrandMark.svelte";

	interface SiteFooterProps {
		healthUrl: string;
	}

	let { healthUrl }: SiteFooterProps = $props();
</script>

<footer class="site-footer" data-component="site-footer">
	<div class="site-footer__inner">
		<div>
			<BrandMark href="/#home" aria-label="archive — Back to top" />
			<p class="site-footer__statement">A public reading room for pages that would otherwise disappear.</p>
		</div>

		<div class="site-footer__links">
			<div>
				<h2>Archive</h2>
				<a href="/#browse">Browse snapshots</a>
				<a href="/archive/new">Archive a page</a>
			</div>
			<div>
				<h2>Context</h2>
				<a href="/#about">About the record</a>
				<a href="/#about">How records work</a>
			</div>
			<div>
				<h2>Technical</h2>
				<a href={healthUrl} target="_blank" rel="noreferrer">System health&nbsp;↗</a>
			</div>
		</div>

		<div class="site-footer__meta">
			<span>PUBLIC INDEX · NO ACCOUNT REQUIRED</span>
			<span>TIMES SHOWN WITH YOUR LOCAL OFFSET</span>
		</div>
	</div>
</footer>

<style>
	.site-footer {
		border-top: 2px solid var(--reading-ink);
		background: var(--archive-layer);
	}

	.site-footer__inner {
		display: grid;
		width: min(100%, 86rem);
		grid-template-columns: minmax(15rem, 0.8fr) minmax(0, 1.2fr);
		gap: 4rem 6rem;
		margin-inline: auto;
		padding: 4rem clamp(1rem, 4vw, 4rem) 2rem;
	}

	.site-footer__statement {
		max-width: 24rem;
		margin: 1.5rem 0 0;
		color: var(--marginal-note);
		font-family: var(--font-editorial);
		font-size: 1.25rem;
		line-height: 1.42;
	}

	.site-footer__links {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 2rem;
	}

	.site-footer__links > div {
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		gap: 0.625rem;
	}

	.site-footer__links h2 {
		margin: 0 0 0.375rem;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.09em;
		text-transform: uppercase;
	}

	.site-footer__links a {
		min-height: 2.75rem;
		border: 0;
		background: transparent;
		padding: 0.625rem 0;
		color: var(--reading-ink);
		font-size: 0.9375rem;
		text-align: left;
		text-decoration: underline;
		text-decoration-thickness: 0.08em;
		text-underline-offset: 0.16em;
		cursor: pointer;
	}

	.site-footer__links a:hover {
		color: var(--register-mark);
		text-decoration-thickness: 0.14em;
	}

	.site-footer__meta {
		display: flex;
		grid-column: 1 / -1;
		justify-content: space-between;
		gap: 1rem;
		border-top: 1px solid var(--border-trace);
		padding-top: 1.5rem;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.6875rem;
		letter-spacing: 0.06em;
	}

	@media (max-width: 48rem) {
		.site-footer__inner {
			grid-template-columns: 1fr;
			gap: 3rem;
		}

		.site-footer__links {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}

		.site-footer__meta {
			grid-column: auto;
			flex-direction: column;
			gap: 0.5rem;
		}
	}

	@media (max-width: 34rem) {
		.site-footer__links {
			grid-template-columns: 1fr;
		}
	}
</style>
