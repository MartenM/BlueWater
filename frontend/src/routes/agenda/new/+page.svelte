<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { AgendaForm } from '$lib';
	import type { UpsertAgendaItemRequest } from '$lib/api/apiClient';

	async function handleCreate(request: UpsertAgendaItemRequest) {
		const result = await apiClient.agendaPOST(request);
		goto(resolve('/agenda/[id]', { id: result.id }));
	}
</script>

<div class="mx-auto max-w-2xl px-4 py-12 sm:px-6 lg:px-8">
	<h1 class="text-2xl font-bold text-gray-900">Nieuw agendapunt</h1>
	<div class="mt-6">
		<AgendaForm submitLabel="Plaatsen" onSubmit={handleCreate} />
	</div>
</div>
