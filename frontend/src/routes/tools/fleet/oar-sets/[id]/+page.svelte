<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { HasPermission, ConfirmDialog, Button, Spinner, breadcrumbs } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { OarSetDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let oarSet = $state<OarSetDto | null>(null);
	let error = $state(false);
	let loading = $state(true);
	let deleteDialog = $state<HTMLDialogElement>();

	onMount(async () => {
		try {
			oarSet = await apiClient.oarSetsGET(params.id);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (oarSet) {
			breadcrumbs.set([
				{ label: 'Vloot', href: '/tools/fleet' },
				{ label: 'Riemstellen', href: '/tools/fleet/oar-sets' },
				{ label: oarSet.name }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		await apiClient.oarSetsDELETE(params.id);
		goto(resolve('/tools/fleet/oar-sets'));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !oarSet}
	<p class="text-sm text-gray-600">Riemstel kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<div>
			<a href={resolve('/tools/fleet/oar-sets')} class="text-sm text-gray-500 hover:underline">
				← Riemstellen
			</a>
			<h1 class="mt-1 text-2xl font-bold text-gray-900">{oarSet.name}</h1>
		</div>
		<HasPermission permission={BluePermission.FleetModify}>
			<div class="flex gap-3">
				<Button
					href={resolve('/tools/fleet/oar-sets/[id]/edit', { id: params.id })}
					variant="secondary"
					size="sm"
				>
					Bewerken
				</Button>
				<Button variant="danger" size="sm" onclick={() => deleteDialog?.showModal()}>
					Verwijderen
				</Button>
			</div>
		</HasPermission>
	</div>

	<dl class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
		<div>
			<dt class="text-sm font-medium text-gray-500">Fabrikant</dt>
			<dd class="mt-1 text-sm text-gray-900">{oarSet.manufacturerName ?? '—'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Type</dt>
			<dd class="mt-1 text-sm text-gray-900">{oarSet.scull ? 'Scull' : 'Sweep'}</dd>
		</div>
	</dl>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{oarSet.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>
{/if}
