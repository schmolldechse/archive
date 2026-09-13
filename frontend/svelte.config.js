import adapter from 'svelte-adapter-bun';

/** @type {import('@sveltejs/kit').Config} */
const config = {
  compilerOptions: {
    experimental: {
      async: true
    }
  },
  kit: {
    adapter: adapter(),
    alias: {
      $api: './src/lib/api'
    },
    experimental: {
      remoteFunctions: true
    }
  }
};

export default config;
