<script lang="ts">
	import { page } from "$app/state";
	import AdminSectionHeading from "$lib/components/admin/AdminSectionHeading.svelte";
	import UserSearch from "$lib/components/admin/UserSearch.svelte";
	import UserTable from "$lib/components/admin/UserTable.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head><title>Users · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<AdminSectionHeading title="Users" description="Manage roles and individual permissions." section="users" />

{#key page.url.search}<UserSearch filters={data.filters} />{/key}
<UserTable
	users={data.users}
	filtered={Boolean(data.filters.q || data.filters.role)}
	canManage={data.admin.roles || data.admin.permissions}
/>
