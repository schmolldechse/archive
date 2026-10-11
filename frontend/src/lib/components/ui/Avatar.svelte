<script module lang="ts">
	import type { Snippet } from "svelte";
	import type { HTMLAttributes, HTMLImgAttributes } from "svelte/elements";

	export type AvatarSize = "compact" | "default" | "large";
	export type AvatarState = "idle" | "loading" | "loaded" | "error";

	export interface AvatarFallbackContext {
		state: AvatarState;
		initials: string | undefined;
	}

	export type AvatarImageProps = Pick<
		HTMLImgAttributes,
		"srcset" | "sizes" | "loading" | "decoding" | "fetchpriority" | "crossorigin" | "referrerpolicy" | "onload" | "onerror"
	>;

	type NativeAvatarProps = Omit<
		HTMLAttributes<HTMLSpanElement>,
		| "children"
		| "role"
		| "aria-label"
		| "aria-labelledby"
		| "aria-hidden"
		| "aria-busy"
		| "aria-live"
		| "tabindex"
		| "contenteditable"
	>;

	type AvatarAccessibilityProps = { decorative: true; alt?: never } | { decorative?: false; alt: string };

	export type AvatarProps = NativeAvatarProps &
		AvatarAccessibilityProps & {
			src?: string | null;
			size?: AvatarSize;
			initials?: string;
			fallback?: Snippet<[AvatarFallbackContext]>;
			imageProps?: AvatarImageProps;
		};
</script>

<script lang="ts">
	import UserRound from "@lucide/svelte/icons/user-round";
	import { untrack } from "svelte";
	import type { Attachment } from "svelte/attachments";
	import { dev } from "$app/environment";

	let {
		src,
		size = "default",
		decorative = false,
		alt,
		initials,
		fallback,
		imageProps,
		class: className,
		...restProps
	}: AvatarProps = $props();

	type ImageAttempt = { element: HTMLImageElement; key: string; result: "loaded" | "error" | null };
	let attempt = $state.raw<ImageAttempt | null>(null);

	const source = $derived(src?.trim() || undefined);
	const fallbackInitials = $derived(initials?.trim() || undefined);
	const imageOptions = $derived({
		srcset: imageProps?.srcset?.trim() || undefined,
		sizes: imageProps?.sizes?.trim() || undefined,
		loading: imageProps?.loading ?? "eager",
		decoding: imageProps?.decoding ?? "async",
		fetchpriority: imageProps?.fetchpriority ?? "auto",
		crossorigin: imageProps?.crossorigin,
		referrerpolicy: imageProps?.referrerpolicy ?? "no-referrer"
	});
	const requestKey = $derived(
		JSON.stringify([source, imageOptions.srcset, imageOptions.sizes, imageOptions.crossorigin, imageOptions.referrerpolicy])
	);
	const imageState: AvatarState = $derived(
		!source ? "idle" : attempt?.key === requestKey ? (attempt.result ?? "loading") : "loading"
	);
	const imageDimension = $derived(size === "compact" ? 32 : size === "large" ? 64 : 40);
	const rootProps = $derived.by(() => {
		const attributes: HTMLAttributes<HTMLSpanElement> = { ...restProps };
		delete attributes.tabindex;
		delete attributes.contenteditable;
		return attributes;
	});

	// Only the actual mounted image supplies results, including requests completed before hydration.
	function attachImage(key: string): Attachment<HTMLImageElement> {
		return (element) => {
			untrack(() => {
				attempt = { element, key, result: element.complete ? (element.naturalWidth > 0 ? "loaded" : "error") : null };
			});
			return () => {
				untrack(() => {
					if (attempt?.element === element) attempt = null;
				});
			};
		};
	}

	const handleLoad: NonNullable<HTMLImgAttributes["onload"]> = (event) => {
		if (attempt?.element !== event.currentTarget || attempt.key !== requestKey) return;
		attempt = { ...attempt, result: attempt.element.naturalWidth > 0 ? "loaded" : "error" };
		imageProps?.onload?.(event);
	};

	const handleError: NonNullable<HTMLImgAttributes["onerror"]> = (event) => {
		if (attempt?.element !== event.currentTarget || attempt.key !== requestKey) return;
		attempt = { ...attempt, result: "error" };
		imageProps?.onerror?.(event);
	};

	const validateAccessibleName: Attachment<HTMLSpanElement> = () => {
		if (!decorative && !alt?.trim()) {
			console.warn('Avatar requires a non-empty "alt" when it is informative. Use decorative={true} for a decorative image.');
		}
	};
</script>

<span
	{...rootProps}
	{@attach dev && validateAccessibleName}
	class={[
		"avatar border-border-trace bg-archive-layer text-reading-ink inline-flex shrink-0 items-center justify-center overflow-hidden border font-sans",
		className
	]}
	role={decorative ? undefined : "img"}
	aria-label={decorative ? undefined : alt?.trim()}
	aria-labelledby={undefined}
	aria-hidden={decorative ? "true" : undefined}
	aria-busy={undefined}
	aria-live={undefined}
	data-component="avatar"
	data-state={imageState}
	data-size={size}
	data-decorative={decorative ? "" : undefined}
>
	<span class="avatar__fallback" data-avatar-fallback aria-hidden="true" hidden={imageState === "loaded"}>
		{#if fallback}
			{@render fallback({ state: imageState, initials: fallbackInitials })}
		{:else if fallbackInitials}
			<span class="avatar__initials" dir="auto">{fallbackInitials}</span>
		{:else}
			<UserRound class="text-marginal-note" aria-hidden="true" />
		{/if}
	</span>
	{#if source}
		{#key requestKey}
			<img
				{@attach attachImage(requestKey)}
				{...imageOptions}
				src={source}
				alt=""
				aria-hidden="true"
				width={imageDimension}
				height={imageDimension}
				data-avatar-image
				onload={handleLoad}
				onerror={handleError}
			/>
		{/key}
	{/if}
</span>

<style>
	.avatar {
		--avatar-size: 2.5rem;
		position: relative;
		inline-size: max(var(--avatar-size), calc(2.25em + 0.125rem + 2px));
		block-size: max(var(--avatar-size), calc(2.25em + 0.125rem + 2px));
		box-sizing: border-box;
		border-radius: 50%;
		font-size: 0.875rem;
		font-weight: 600;
		line-height: 1;
		vertical-align: middle;
	}

	.avatar[data-size="compact"] {
		--avatar-size: 2rem;
		font-size: 0.75rem;
	}

	.avatar[data-size="large"] {
		--avatar-size: 4rem;
		font-size: 1.25rem;
	}

	.avatar__fallback {
		display: flex;
		inline-size: 100%;
		block-size: 100%;
		min-inline-size: 0;
		align-items: center;
		justify-content: center;
		overflow: hidden;
		padding: 0.125rem;
	}

	.avatar__fallback[hidden] {
		display: none;
	}

	.avatar__initials {
		max-inline-size: 100%;
		overflow: hidden;
		white-space: nowrap;
	}

	.avatar__fallback :global(svg) {
		inline-size: 60%;
		block-size: 60%;
		flex: none;
		stroke-width: 1.5;
	}

	.avatar img {
		position: absolute;
		inset: 0;
		inline-size: 100%;
		block-size: 100%;
		object-fit: cover;
		object-position: center;
		opacity: 0;
	}

	.avatar[data-state="loaded"] img {
		opacity: 1;
	}
</style>
