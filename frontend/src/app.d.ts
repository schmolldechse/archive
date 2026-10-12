declare global {
	namespace App {
		interface Locals {
			session: import("$api").AuthenticationSession | null;
		}
	}
}

export {};
