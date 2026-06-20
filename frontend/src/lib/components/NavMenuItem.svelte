<script lang="ts">
	import ChevronDown from '@lucide/svelte/icons/chevron-down';
	import type { NavItem } from '$lib/navigation';

	let {
		item,
		open,
		onOpenChange
	}: { item: NavItem; open: boolean; onOpenChange: (open: boolean) => void } = $props();

	let root: HTMLLIElement;
	let closeTimeout: ReturnType<typeof setTimeout> | undefined;

	function openNow() {
		clearTimeout(closeTimeout);
		onOpenChange(true);
	}

	function closeWithDelay() {
		clearTimeout(closeTimeout);
		closeTimeout = setTimeout(() => onOpenChange(false), 250);
	}

	function handleFocusOut(event: FocusEvent) {
		if (!root.contains(event.relatedTarget as Node)) {
			onOpenChange(false);
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (open && event.key === 'Escape') {
			onOpenChange(false);
		}
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- item/child hrefs are data-driven (will come from an API), not static route literals resolve() can check -->
<svelte:window onkeydown={handleKeydown} />

<li
	class="relative"
	bind:this={root}
	onmouseenter={openNow}
	onmouseleave={closeWithDelay}
	onfocusin={openNow}
	onfocusout={handleFocusOut}
>
	{#if item.children?.length}
		<a
			href={item.href}
			class="flex items-center gap-1 rounded-md px-3 py-2 text-base font-medium text-black hover:bg-gray-100 hover:text-primary-hover"
			aria-expanded={open}
		>
			{item.label}
			<ChevronDown class="size-4 transition-transform {open ? 'rotate-180' : ''}" />
		</a>
		{#if open}
			<ul
				class="absolute left-0 z-10 mt-1 min-w-48 rounded-md border border-gray-200 bg-white py-1 shadow-sm"
			>
				{#each item.children as child (child.label)}
					<li>
						<a
							href={child.href}
							class="block px-3 py-2 text-base text-black hover:bg-gray-100"
							onclick={() => onOpenChange(false)}
						>
							{child.label}
						</a>
					</li>
				{/each}
			</ul>
		{/if}
	{:else}
		<a
			href={item.href}
			class="block rounded-md px-3 py-2 text-base font-medium text-black hover:bg-gray-100 hover:text-primary-hover"
		>
			{item.label}
		</a>
	{/if}
</li>
