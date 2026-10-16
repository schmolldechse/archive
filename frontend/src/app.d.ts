declare global {
	namespace App {
		interface Locals {
			session: import("$api").AuthenticationSessionResponse | null;
		}
	}
}

export {};
