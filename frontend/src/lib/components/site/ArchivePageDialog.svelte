<script lang="ts">
	import ArrowRight from "@lucide/svelte/icons/arrow-right";

	import Button from "$lib/components/ui/Button.svelte";
	import {
		DialogActions,
		DialogClose,
		DialogContent,
		DialogDescription,
		DialogRoot,
		DialogTitle
	} from "$lib/components/ui/dialog";

	interface ArchivePageDialogProps {
		open?: boolean;
		onContinue?: () => void;
	}

	let { open = $bindable(false), onContinue }: ArchivePageDialogProps = $props();

	function continueToSubmission(): void {
		open = false;
		onContinue?.();
	}
</script>

<DialogRoot bind:open>
	<DialogContent size="small" class="archive-page-dialog">
		<span class="dialog-mark" aria-hidden="true"></span>
		<p class="dialog-label">Separate workflow</p>
		<DialogTitle>Archive a page</DialogTitle>
		<DialogDescription>
			Submissions belong on their own route, keeping this public index focused on finding and reading preserved records.
		</DialogDescription>
		<DialogActions>
			<DialogClose>Stay in the index</DialogClose>
			<Button variant="primary" onclick={continueToSubmission}>
				Continue to submission
				<ArrowRight aria-hidden="true" />
			</Button>
		</DialogActions>
	</DialogContent>
</DialogRoot>

<style>
	:global(.archive-page-dialog) {
		position: relative;
		padding-top: 2rem;
	}

	.dialog-mark {
		display: block;
		width: 3rem;
		height: 3px;
		margin-bottom: 1.5rem;
		background: var(--time-marker);
	}

	.dialog-label {
		margin: 0 0 0.5rem;
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.09em;
		text-transform: uppercase;
	}
</style>
