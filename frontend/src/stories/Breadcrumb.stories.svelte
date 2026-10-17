<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";
	import { Breadcrumb as BreadcrumbMeta } from "$lib/components/ui/breadcrumb";

	const { Story } = defineMeta({
		title: "Components/Breadcrumb",
		component: BreadcrumbMeta,
		parameters: {
			docs: {
				description: {
					component: "A semantic, composable navigation trail with native links and a marked current page."
				}
			}
		}
	});
</script>

<script lang="ts">
	import {
		Breadcrumb,
		BreadcrumbEllipsis,
		BreadcrumbItem,
		BreadcrumbLink,
		BreadcrumbList,
		BreadcrumbPage,
		BreadcrumbSeparator
	} from "$lib/components/ui/breadcrumb";
</script>

<Story name="Basic trail" asChild>
	<Breadcrumb>
		<BreadcrumbList>
			<BreadcrumbItem><BreadcrumbLink href="#home">Home</BreadcrumbLink></BreadcrumbItem>
			<BreadcrumbSeparator />
			<BreadcrumbItem><BreadcrumbLink href="#section">Section</BreadcrumbLink></BreadcrumbItem>
			<BreadcrumbSeparator />
			<BreadcrumbItem><BreadcrumbPage>Current page</BreadcrumbPage></BreadcrumbItem>
		</BreadcrumbList>
	</Breadcrumb>
</Story>

<Story name="Collapsed trail and custom separator" asChild>
	<div class="constrained">
		<Breadcrumb aria-label="Collapsed breadcrumb">
			<BreadcrumbList>
				<BreadcrumbItem><BreadcrumbLink href="#home">Home</BreadcrumbLink></BreadcrumbItem>
				<BreadcrumbSeparator><span>/</span></BreadcrumbSeparator>
				<BreadcrumbItem><BreadcrumbEllipsis /></BreadcrumbItem>
				<BreadcrumbSeparator><span>/</span></BreadcrumbSeparator>
				<BreadcrumbItem><BreadcrumbPage>A long current page title that wraps on narrow screens</BreadcrumbPage></BreadcrumbItem>
			</BreadcrumbList>
		</Breadcrumb>
	</div>
</Story>

<style>
	.constrained {
		max-width: 17rem;
	}
</style>
