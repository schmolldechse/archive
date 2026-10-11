<script lang="ts">
	import { untrack } from "svelte";
	import { ToastProvider, ToastViewport, createToastController, type ToastOptions } from "$lib/components/ui/toast";

	let { notice }: { notice: ToastOptions } = $props();
	const toast = createToastController();
	// The parent keys this component by the server result, so each notice is added once.
	untrack(() => toast.add({ ...notice, id: "login-notice" }));
</script>

<ToastProvider controller={toast} limit={1}>
	<ToastViewport />
</ToastProvider>
