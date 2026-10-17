<script lang="ts">
	import { page } from "$app/state";
	import Button from "$lib/components/ui/Button.svelte";
</script>

<svelte:head><title>{page.status} · Administration · Archive</title></svelte:head>
<section class="admin-error" aria-labelledby="admin-error-title">
	<p class="record-value">{page.status}</p>
	<h1 id="admin-error-title">
		{page.status === 403 ? "Access unavailable" : page.status === 404 ? "User unavailable" : "Administration unavailable"}
	</h1>
	<p>{page.error?.message}</p>
	<Button href={page.status === 403 ? "/admin" : "/admin/users"}
		>{page.status === 403 ? "Back to administration" : "Back to users"}</Button
	>
</section>

<style>
	.admin-error {
		width: min(100% - 2rem, 60rem);
		margin: 4rem auto;
	}
	.admin-error h1 {
		font-size: 2.5rem;
	}
</style>
