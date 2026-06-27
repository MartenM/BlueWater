<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { HasPermission, ConfirmDialog, Button, Spinner, breadcrumbs } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { EquipmentDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let equipment = $state<EquipmentDto | null>(null);
	let error = $state(false);
	let loading = $state(true);
	let deleteDialog = $state<HTMLDialogElement>();

	onMount(async () => {
		try {
			equipment = await apiClient.fleetGET2(params.id);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (equipment) {
			breadcrumbs.set([{ label: 'Vloot', href: '/tools/fleet' }, { label: equipment.name }]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		await apiClient.fleetDELETE(params.id);
		goto(resolve('/tools/fleet'));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !equipment}
	<p class="text-sm text-gray-600">Materiaal kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<h1 class="text-2xl font-bold text-gray-900">{equipment.name}</h1>
		<HasPermission permission={BluePermission.FleetModify}>
			<div class="flex gap-3">
				<Button
					href={resolve('/tools/fleet/[id]/edit', { id: params.id })}
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
		{#if equipment.description}
			<div class="sm:col-span-2">
				<dt class="text-sm font-medium text-gray-500">Omschrijving</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.description}</dd>
			</div>
		{/if}
		<div>
			<dt class="text-sm font-medium text-gray-500">Type</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipment.equipmentTypeName ?? '—'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Fabrikant</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipment.manufacturerName ?? '—'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Riemstel</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipment.oarSetName ?? '—'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Vrije vloot</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipment.freeFleet ? 'Ja' : 'Nee'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Buiten gebruik</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipment.outOfOrder ? 'Ja' : 'Nee'}</dd>
		</div>
		{#if equipment.rowersWeight}
			<div>
				<dt class="text-sm font-medium text-gray-500">Gewicht roeiers (kg)</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.rowersWeight}</dd>
			</div>
		{/if}
		{#if equipment.rowersWeightMax}
			<div>
				<dt class="text-sm font-medium text-gray-500">Max. gewicht roeiers (kg)</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.rowersWeightMax}</dd>
			</div>
		{/if}
		{#if equipment.dateBuild}
			<div>
				<dt class="text-sm font-medium text-gray-500">Bouwjaar</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.dateBuild}</dd>
			</div>
		{/if}
		{#if equipment.dateBought}
			<div>
				<dt class="text-sm font-medium text-gray-500">Aanschafdatum</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.dateBought}</dd>
			</div>
		{/if}
		{#if equipment.dateSold}
			<div>
				<dt class="text-sm font-medium text-gray-500">Verkoopdatum</dt>
				<dd class="mt-1 text-sm text-gray-900">{equipment.dateSold}</dd>
			</div>
		{/if}
	</dl>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{equipment.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>
{/if}
