<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { HasPermission, Spinner } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { ExamTypeDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<ExamTypeDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	onMount(async () => {
		try {
			items = await apiClient.typesAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Examentypes</h1>
	<HasPermission permission={BluePermission.ExamsModify}>
		<a
			href={resolve('/tools/exams/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuw examentype
		</a>
	</HasPermission>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Examentypes konden niet worden geladen.</p>
{:else}
	<div class="mt-6 divide-y divide-gray-200 border-t border-gray-200">
		{#each items as item (item.id)}
			<a
				href={resolve('/tools/exams/[id]', { id: item.id })}
				class="flex items-center justify-between py-3 hover:bg-gray-50"
			>
				<div>
					<p class="font-medium text-gray-900">{item.name}</p>
					{#if item.description}
						<p class="text-sm text-gray-500">{item.description}</p>
					{/if}
				</div>
			</a>
		{:else}
			<p class="py-6 text-sm text-gray-500">Geen examentypes gevonden.</p>
		{/each}
	</div>
{/if}
