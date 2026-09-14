import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLButtonAttributes, HTMLLiAttributes, HTMLOlAttributes } from "svelte/elements";

export type ToastVariant = "neutral" | "info" | "success" | "warning" | "error";
export type ToastPriority = "normal" | "high";
export type ToastPosition = "top-start" | "top-end" | "bottom-start" | "bottom-end";

export type ToastActionOptions = {
	label: string;
	onAction: () => void | Promise<void>;
	dismissOnAction?: boolean;
};

export type ToastOptions = {
	id?: string;
	title: string;
	description?: string;
	variant?: ToastVariant;
	priority?: ToastPriority;
	duration?: number | null;
	action?: ToastActionOptions;
	dismissible?: boolean;
};

export type ToastUpdate = Partial<Omit<ToastOptions, "id">>;

export type ToastRecord = Readonly<{
	id: string;
	title: string;
	description?: string;
	variant: ToastVariant;
	priority: ToastPriority;
	duration: number | null;
	action?: ToastActionOptions;
	dismissible: boolean;
	revision: number;
}>;

export type ToastController = {
	readonly toasts: readonly ToastRecord[];
	add(options: ToastOptions): string;
	update(id: string, update: ToastUpdate): boolean;
	dismiss(id: string): boolean;
	dismissAll(): void;
};

export interface ToastProviderProps {
	controller?: ToastController;
	limit?: number;
	defaultDuration?: number | null;
	pauseWhenPageHidden?: boolean;
	label?: string;
	children: Snippet<[ToastController]>;
}

export interface ToastViewportProps extends Omit<HTMLOlAttributes, "children"> {
	position?: ToastPosition;
	hotkey?: string;
	children?: Snippet<[ToastRecord]>;
	ref?: HTMLOListElement | null;
}

export interface ToastRootProps extends Omit<HTMLLiAttributes, "children"> {
	children: Snippet;
	ref?: HTMLLIElement | null;
}

export interface ToastTitleProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children?: Snippet;
	ref?: HTMLDivElement | null;
}

export interface ToastDescriptionProps extends Omit<HTMLAttributes<HTMLDivElement>, "children"> {
	children?: Snippet;
	ref?: HTMLDivElement | null;
}

export interface ToastActionProps extends Omit<HTMLButtonAttributes, "children" | "type"> {
	children?: Snippet;
	ref?: HTMLButtonElement | null;
}

export interface ToastCloseProps extends Omit<HTMLButtonAttributes, "children" | "type"> {
	children?: Snippet;
	ref?: HTMLButtonElement | null;
}
