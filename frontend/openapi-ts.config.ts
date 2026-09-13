import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig({
	input: "http://localhost:5200/openapi/v1.json",
	output: {
		path: "src/lib/api",
		clean: true
	},
	plugins: [
		{
			name: "@hey-api/typescript",
			enums: {
				mode: "typescript",
				case: "SCREAMING_SNAKE_CASE"
			}
		}
	]
});
