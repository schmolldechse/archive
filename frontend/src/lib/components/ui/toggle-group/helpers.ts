import type { ClassValue } from "svelte/elements";

export function mergeClasses(...classes: Array<ClassValue | null | undefined>): ClassValue {
	return classes.filter(Boolean) as ClassValue;
}

export function stringArraysEqual(left: string[], right: string[]): boolean {
	return left.length === right.length && left.every((entry, index) => entry === right[index]);
}
