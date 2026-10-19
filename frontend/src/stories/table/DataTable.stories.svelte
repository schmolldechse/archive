<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";
	import DataTableExample from "./DataTableExample.svelte";

	const { Story } = defineMeta({
		title: "Components/DataTable",
		component: DataTableExample,
		parameters: {
			docs: {
				description: {
					component:
						"A local data table for complete collections. Owns filtering, stable single-column sorting, pagination and optional page-scoped checkbox selection. Bind state as one source of truth; onStateChange reports user transitions only. Generic columns preserve the row type in accessors and cell snippets. No routing or API requests are performed. See the Table gallery for the underlying native primitives."
				}
			}
		}
	});
</script>

<Story name="Defaults" args={{ mode: "defaults" }} />
<Story name="Local data" args={{ mode: "local" }} />
<Story name="Sorting" args={{ mode: "sorting" }} />
<Story name="Filters" args={{ mode: "filters" }} />
<Story name="Pagination" args={{ mode: "pagination" }} />
<Story name="Selection" args={{ mode: "selection" }} />
<Story name="State binding" args={{ mode: "binding" }} />
<Story name="Data changes" args={{ mode: "changes" }} />
<Story name="Content snippets" args={{ mode: "snippets" }} />
<Story name="Loading" args={{ mode: "loading" }} />
<Story name="Error" args={{ mode: "error" }} />
<Story name="Empty" args={{ mode: "empty" }} />
<Story name="No results" args={{ mode: "no-results" }} />
<Story name="Responsive layout" args={{ mode: "responsive" }} />
