<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { Button } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import type { UserDto } from '$lib/api/apiClient';
	import BlueAlert from './BlueAlert.svelte';

	let {
		instanceId,
		memberUserIds,
		readonly = false
	}: { instanceId: string; memberUserIds: string[]; readonly?: boolean } = $props();

	let members = $state<UserDto[]>([]);
	let loadError = $state(false);
	let search = $state('');
	let searchResults = $state<UserDto[]>([]);
	let actionError = $state<string | null>(null);
	let busy = $state(false);

	async function loadMembers(ids: string[]) {
		try {
			members = await Promise.all(ids.map((id) => apiClient.getUser(id)));
			loadError = false;
		} catch {
			loadError = true;
		}
	}

	onMount(() => {
		loadMembers(memberUserIds);
	});

	async function handleSearchSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!search.trim()) {
			searchResults = [];
			return;
		}
		try {
			const result = await apiClient.listUsers(1, 10, search);
			searchResults = result.items.filter((u) => !members.some((m) => m.id === u.id));
		} catch {
			actionError = 'Zoeken is mislukt. Probeer het later opnieuw.';
		}
	}

	async function addMember(user: UserDto) {
		busy = true;
		actionError = null;
		try {
			await apiClient.usersPOST(instanceId, user.id);
			members = [...members, user];
			searchResults = searchResults.filter((u) => u.id !== user.id);
		} catch {
			actionError = 'Toevoegen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function removeMember(user: UserDto) {
		if (!confirm(`${user.fullname} uit deze groep verwijderen?`)) return;
		busy = true;
		actionError = null;
		try {
			await apiClient.usersDELETE(instanceId, user.id);
			members = members.filter((m) => m.id !== user.id);
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<div>
	<h2 class="text-sm font-semibold text-gray-700">Leden</h2>

	{#if loadError}
		<p class="mt-2 text-sm text-gray-600">Leden konden niet worden geladen.</p>
	{:else}
		<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
			{#each members as member (member.id)}
				<div class="flex items-center justify-between py-2">
					<span class="text-sm text-gray-900">{member.fullname}</span>
					{#if !readonly}
						<button
							type="button"
							disabled={busy}
							onclick={() => removeMember(member)}
							class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
						>
							Verwijderen
						</button>
					{/if}
				</div>
			{:else}
				<p class="py-2 text-sm text-gray-500">Geen leden.</p>
			{/each}
		</div>
	{/if}

	{#if !readonly}
		<form class="mt-4 flex gap-2" onsubmit={handleSearchSubmit}>
			<input
				type="search"
				placeholder="Zoek gebruiker om toe te voegen"
				bind:value={search}
				class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
			/>
			<Button type="submit" variant="secondary" size="sm">Zoeken</Button>
		</form>

		{#if searchResults.length > 0}
			<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
				{#each searchResults as result (result.id)}
					<div class="flex items-center justify-between py-2">
						<span class="text-sm text-gray-900">{result.fullname}</span>
						<button
							type="button"
							disabled={busy}
							onclick={() => addMember(result)}
							class="text-sm font-medium text-primary-hover hover:underline disabled:opacity-60"
						>
							Toevoegen
						</button>
					</div>
				{/each}
			</div>
		{/if}
	{/if}

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
