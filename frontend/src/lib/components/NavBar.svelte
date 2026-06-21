<script lang="ts">
	import { resolve } from '$app/paths';
	import { navItems } from '$lib/navigation';
	import AuthNudge from './AuthNudge.svelte';
	import NavMenuItem from './NavMenuItem.svelte';

	let openLabel = $state<string | null>(null);
</script>

<header class="relative">
	<nav class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
		<a href={resolve('/')} class="flex items-center">
			<img src="/images/logo/crop/Logo_Gyas_Totaal.svg" alt="Club logo" class="h-15 w-auto" />
		</a>
		<ul class="flex items-center gap-1">
			{#each navItems as item (item.label)}
				<NavMenuItem
					{item}
					open={openLabel === item.label}
					onOpenChange={(open) => {
						if (open) {
							openLabel = item.label;
						} else if (openLabel === item.label) {
							openLabel = null;
						}
					}}
				/>
			{/each}
		</ul>
	</nav>
	<div
		class="h-2"
		style="background-image: linear-gradient(180deg, var(--color-primary) 50%, color-mix(in srgb, var(--color-primary) 70%, white) 50%);"
	></div>
	<div class="relative mx-auto max-w-7xl">
		<AuthNudge />
	</div>
</header>
