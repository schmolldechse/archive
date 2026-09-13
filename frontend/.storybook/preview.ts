import { withThemeByDataAttribute } from "@storybook/addon-themes";
import type { Preview, Renderer } from "@storybook/sveltekit";
import "../src/app.css";

const preview: Preview = {
	decorators: [
		withThemeByDataAttribute<Renderer>({
			themes: {
				light: "light",
				dark: "dark"
			},
			defaultTheme: "light",
			attributeName: "data-theme"
		})
	],
	parameters: {
		layout: "padded",
		controls: {
			matchers: {
				color: /(background|color)$/i,
				date: /Date$/i
			}
		},
		options: {
			storySort: {
				order: ["Grundlagen", "Komponenten"]
			}
		}
	}
};

export default preview;
