<script lang="ts">
	import { untrack } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import { HasPermission, ConfirmDialog, Button, breadcrumbs } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { data, params }: PageProps = $props();

	let manufacturer = $state(untrack(() => data.manufacturer));
	let error = $state(untrack(() => data.error));
	let deleteDialog = $state<HTMLDialogElement>();

	$effect(() => {
		if (manufacturer) {
			breadcrumbs.set([
				{ label: 'Vloot', href: '/tools/fleet' },
				{ label: 'Fabrikanten', href: '/tools/fleet/manufacturers' },
				{ label: manufacturer.name }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		await apiClient.manufacturersDELETE(params.id);
		goto(resolve('/tools/fleet/manufacturers'));
	}
</script>

{#if error || !manufacturer}
	<p class="text-sm text-gray-600">Fabrikant kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<div>
			<a href={resolve('/tools/fleet/manufacturers')} class="text-sm text-gray-500 hover:underline">
				← Fabrikanten
			</a>
			<h1 class="mt-1 text-2xl font-bold text-gray-900">{manufacturer.name}</h1>
		</div>
		<HasPermission permission={BluePermission.FleetModify}>
			<div class="flex gap-3">
				<Button
					href={resolve('/tools/fleet/manufacturers/[id]/edit', { id: params.id })}
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

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{manufacturer.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>
{/if}
