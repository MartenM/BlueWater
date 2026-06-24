<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { apiClient } from '$lib/api/client';
	import { Button } from '$lib';
	import { AssignPermissionRequest, BluePermission } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import BlueAlert from './BlueAlert.svelte';

	let {
		instanceId,
		permissions: initialPermissions,
		readonly = false
	}: { instanceId: string; permissions: BluePermission[]; readonly?: boolean } = $props();

	let permissions = $state<BluePermission[]>(untrack(() => [...initialPermissions]));
	let availablePermissions = $state<BluePermission[]>([]);
	let selected = $state<BluePermission | ''>('');
	let actionError = $state<string | null>(null);
	let busy = $state(false);

	const remaining = $derived(availablePermissions.filter((p) => !permissions.includes(p)));

	onMount(async () => {
		try {
			availablePermissions = await apiClient.permissionsAll();
		} catch {
			availablePermissions = [];
		}
	});

	async function addPermission(event: SubmitEvent) {
		event.preventDefault();
		if (!selected) return;
		const permission = selected;
		busy = true;
		actionError = null;
		try {
			await apiClient.permissionsPOST(instanceId, new AssignPermissionRequest({ permission }));
			permissions = [...permissions, permission];
			selected = '';
		} catch {
			actionError = 'Toevoegen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function removePermission(permission: BluePermission) {
		if (!confirm(`Permissie "${permission}" verwijderen?`)) return;
		busy = true;
		actionError = null;
		try {
			await apiClient.permissionsDELETE(instanceId, permission);
			permissions = permissions.filter((p) => p !== permission);
		} catch {
			actionError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}
</script>

<div>
	<h2 class="text-sm font-semibold text-gray-700">Permissies</h2>

	<div class="mt-2 flex flex-wrap gap-2">
		{#each permissions as permission (permission)}
			<span
				class="inline-flex items-center gap-1.5 rounded-md border border-gray-200 bg-gray-50 px-2 py-1 text-sm text-gray-900"
			>
				{permission}
				{#if !readonly}
					<button
						type="button"
						disabled={busy}
						onclick={() => removePermission(permission)}
						class="font-medium text-red-600 hover:underline disabled:opacity-60"
					>
						&times;
					</button>
				{/if}
			</span>
		{:else}
			<p class="text-sm text-gray-500">Geen permissies.</p>
		{/each}
	</div>

	{#if !readonly && remaining.length > 0}
		<form class="mt-4 flex gap-2" onsubmit={addPermission}>
			<select
				bind:value={selected}
				class="flex-1 rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
			>
				<option value="">Selecteer een permissie</option>
				{#each remaining as permission (permission)}
					<option value={permission}>{permission}</option>
				{/each}
			</select>
			<Button type="submit" variant="secondary" size="sm" disabled={!selected || busy}>
				Permissie toevoegen
			</Button>
		</form>
	{/if}

	{#if actionError}
		<div class="mt-3">
			<BlueAlert level={AlertLevel.Danger}>{actionError}</BlueAlert>
		</div>
	{/if}
</div>
