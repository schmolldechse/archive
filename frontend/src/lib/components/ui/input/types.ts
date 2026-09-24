import type { Snippet } from "svelte";
import type { HTMLAttributes, HTMLInputAttributes, HTMLLabelAttributes, HTMLTextareaAttributes } from "svelte/elements";

export type InputState = "default" | "invalid" | "disabled";
export type InputType =
	"text" | "search" | "email" | "url" | "tel" | "password" | "number" | "date" | "time" | "datetime-local" | "file";
export type InputValue = string | number | undefined;
export type InputValueKind = "text" | "record";

type NativeDivProps = Omit<HTMLAttributes<HTMLDivElement>, "children" | "id">;
type NativeLabelProps = Omit<HTMLLabelAttributes, "children" | "for">;
type NativeParagraphProps = Omit<HTMLAttributes<HTMLParagraphElement>, "children" | "id">;
type NativeInputProps = Omit<
	HTMLInputAttributes,
	"aria-describedby" | "aria-invalid" | "children" | "disabled" | "id" | "required" | "type" | "value"
>;

export interface InputRootProps extends NativeDivProps {
	id?: string;
	disabled?: boolean;
	required?: boolean;
	invalid?: boolean;
	hasDescription?: boolean;
	children: Snippet;
	ref?: HTMLDivElement | null;
}

export interface InputLabelProps extends NativeLabelProps {
	children: Snippet;
	ref?: HTMLLabelElement | null;
}

interface InputControlBaseProps extends NativeInputProps {
	ref?: HTMLInputElement | null;
	"aria-describedby"?: HTMLInputAttributes["aria-describedby"];
	"data-value-kind"?: InputValueKind;
}

export type InputControlProps = InputControlBaseProps &
	(
		| { type?: Exclude<InputType, "file">; value?: InputValue; onFilesChange?: never }
		| { type: "file"; value?: never; onFilesChange?: (files: File[]) => void }
	);

export interface InputTextareaProps extends Omit<
	HTMLTextareaAttributes,
	"aria-describedby" | "aria-invalid" | "children" | "disabled" | "id" | "required" | "value"
> {
	value?: string;
	ref?: HTMLTextAreaElement | null;
	"aria-describedby"?: HTMLTextareaAttributes["aria-describedby"];
}

export interface InputDescriptionProps extends NativeParagraphProps {
	children: Snippet;
	ref?: HTMLParagraphElement | null;
}

export interface InputErrorProps extends NativeParagraphProps {
	children: Snippet;
	ref?: HTMLParagraphElement | null;
}
