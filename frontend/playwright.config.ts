import { defineConfig } from '@playwright/test';

export default defineConfig({
	testDir: './e2e',
	use: {
		baseURL: 'http://localhost:5174'
	},
	projects: [
		{
			name: 'setup',
			testMatch: /auth\.setup\.ts/
		},
		{
			name: 'chromium',
			use: { storageState: 'e2e/.auth/user.json' },
			dependencies: ['setup']
		}
	],
	webServer: {
		command: 'pnpm dev',
		url: 'http://localhost:5174',
		reuseExistingServer: !process.env.CI,
		env: {
			// Match the frontend's http scheme so the browser treats frontend and backend as
			// same-site; otherwise Chromium's third-party-cookie blocking silently drops the
			// auth cookies the backend sets on login.
			PUBLIC_API_BASE_URL: 'http://localhost:7293'
		}
	}
});
