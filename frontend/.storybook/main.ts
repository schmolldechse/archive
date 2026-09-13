import type { StorybookConfig } from "@storybook/sveltekit";

const config: StorybookConfig = {
	stories: ["../src/stories/**/*.stories.@(js|ts|svelte)"],
	addons: ["@storybook/addon-docs", "@storybook/addon-svelte-csf", "@storybook/addon-themes"],
	framework: {
		name: "@storybook/sveltekit",
		options: {}
	}
};

export default config;
