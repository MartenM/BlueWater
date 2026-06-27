<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { HasPermission, ConfirmDialog, Button, Spinner, breadcrumbs } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { EquipmentTypeDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let equipmentType = $state<EquipmentTypeDto | null>(null);
	let error = $state(false);
	let loading = $state(true);
	let deleteDialog = $state<HTMLDialogElement>();

	onMount(async () => {
		try {
			equipmentType = await apiClient.typesGET2(params.id);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (equipmentType) {
			breadcrumbs.set([
				{ label: 'Vloot', href: '/tools/fleet' },
				{ label: 'Materiaaltypen', href: '/tools/fleet/types' },
				{ label: equipmentType.name }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		await apiClient.typesDELETE2(params.id);
		goto(resolve('/tools/fleet/types'));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !equipmentType}
	<p class="text-sm text-gray-600">Materiaaltype kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<div>
			<a href={resolve('/tools/fleet/types')} class="text-sm text-gray-500 hover:underline">
				← Materiaaltypen
			</a>
			<h1 class="mt-1 text-2xl font-bold text-gray-900">{equipmentType.name}</h1>
		</div>
		<HasPermission permission={BluePermission.FleetModify}>
			<div class="flex gap-3">
				<Button
					href={resolve('/tools/fleet/types/[id]/edit', { id: params.id })}
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
			<dt class="text-sm font-medium text-gray-500">Code</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipmentType.code}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Aantal roeiers</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipmentType.rowersCount}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Boot</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipmentType.isBoat ? 'Ja' : 'Nee'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Scull</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipmentType.scull ? 'Ja' : 'Nee'}</dd>
		</div>
		<div>
			<dt class="text-sm font-medium text-gray-500">Met stuurman</dt>
			<dd class="mt-1 text-sm text-gray-900">{equipmentType.coxed ? 'Ja' : 'Nee'}</dd>
		</div>
	</dl>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{equipmentType.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>
{/if}
