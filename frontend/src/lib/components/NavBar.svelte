<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { navItems } from '$lib/navigation';
	import { logout } from '$lib/auth/auth';
	import { session } from '$lib/auth/session.svelte';
	import NavMenuItem from './NavMenuItem.svelte';

	let openLabel = $state<string | null>(null);

	async function handleLogout() {
		await logout();
		goto(resolve('/'));
	}
</script>

<header>
	<nav class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
		<a href={resolve('/')} class="flex items-center">
			<img src="/images/logo/crop/Logo_Gyas_Totaal.svg" alt="Club logo" class="h-15 w-auto" />
		</a>
		<div class="flex items-center gap-4">
			<ul class="flex items-center gap-1">
				{#each navItems as item (item.label)}
					<NavMenuItem
						{item}
						open={openLabel === item.label}
						onOpenChange={(open) => (openLabel = open ? item.label : null)}
					/>
				{/each}
			</ul>
			{#if session.user}
				<div class="flex items-center gap-2 text-base">
					<span class="text-gray-700">{session.user.email}</span>
					<button
						type="button"
						onclick={handleLogout}
						class="rounded-md px-3 py-2 font-medium text-black hover:bg-gray-100 hover:text-primary-hover"
					>
						Uitloggen
					</button>
				</div>
			{:else}
				<a
					href={resolve('/login')}
					class="rounded-md px-3 py-2 text-base font-medium text-black hover:bg-gray-100 hover:text-primary-hover"
				>
					Inloggen
				</a>
			{/if}
		</div>
	</nav>
	<div
		class="h-2"
		style="background-image: linear-gradient(180deg, var(--color-primary) 50%, color-mix(in srgb, var(--color-primary) 70%, white) 50%);"
	></div>
</header>
