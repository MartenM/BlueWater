<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { AgendaForm } from '$lib';
	import type { AgendaItemDto, UpsertAgendaItemRequest } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let item = $state<AgendaItemDto | null>(null);
	let error = $state(false);

	onMount(async () => {
		try {
			item = await apiClient.agendaGET2(params.id);
		} catch {
			error = true;
		}
	});

	async function handleEdit(request: UpsertAgendaItemRequest) {
		await apiClient.agendaPUT(params.id, request);
		goto(resolve('/agenda/[id]', { id: params.id }));
	}
</script>

<div class="mx-auto max-w-2xl px-4 py-12 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Agendapunt bewerken</h1>
	<div class="mt-6">
		{#if error}
			<p class="text-sm text-gray-600">Agenda kon niet worden geladen.</p>
		{:else if item}
			<AgendaForm {item} submitLabel="Opslaan" onSubmit={handleEdit} />
		{/if}
	</div>
</div>
