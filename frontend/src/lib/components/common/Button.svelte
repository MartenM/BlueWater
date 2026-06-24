<script lang="ts">
	import type { Snippet } from 'svelte';

	type Variant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning';
	type Size = 'sm' | 'md';

	let {
		variant = 'primary',
		size = 'md',
		type = 'button',
		href,
		disabled = false,
		onclick,
		children,
		...rest
	}: {
		variant?: Variant;
		size?: Size;
		type?: 'button' | 'submit' | 'reset';
		href?: string;
		disabled?: boolean;
		onclick?: (event: MouseEvent) => void;
		children: Snippet;
	} = $props();

	const variantStyles: Record<Variant, string> = {
		primary: 'bg-primary text-primary-content hover:bg-primary-hover',
		secondary: 'border border-gray-300 text-gray-700 hover:bg-gray-50',
		success: 'bg-green-600 text-white hover:bg-green-700',
		danger: 'bg-red-600 text-white hover:bg-red-700',
		warning: 'bg-amber-500 text-white hover:bg-amber-600'
	};

	const sizeStyles: Record<Size, string> = {
		sm: 'px-3 py-1.5 text-sm',
		md: 'px-4 py-2'
	};

	const classes = $derived(
		`rounded-md font-medium disabled:opacity-60 ${variantStyles[variant]} ${sizeStyles[size]}`
	);
</script>

{#if href}
	<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- href is caller-supplied (often a resolve() result with appended query params), not a static route literal -->
	<a {href} class={classes} {onclick}>
		{@render children()}
	</a>
{:else}
	<button {type} {disabled} {onclick} class={classes} {...rest}>
		{@render children()}
	</button>
{/if}
