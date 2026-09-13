import type { ClassValue } from "svelte/elements";

export function mergeClasses(...classes: Array<ClassValue | null | undefined>): ClassValue {
	return classes.filter(Boolean) as ClassValue;
}
