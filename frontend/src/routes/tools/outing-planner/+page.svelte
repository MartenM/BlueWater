<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { Spinner, breadcrumbs } from '$lib';
	import { OutingParticipantRole, type OutingOverviewGroupDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let groups = $state<OutingOverviewGroupDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	const roleLabels: Record<OutingParticipantRole, string> = {
		[OutingParticipantRole.None]: 'Geen status',
		[OutingParticipantRole.Rower]: 'Roeier',
		[OutingParticipantRole.Cox]: 'Stuurman/-vrouw',
		[OutingParticipantRole.Coach]: 'Coach',
		[OutingParticipantRole.Reserve]: 'Reserve',
		[OutingParticipantRole.Unavailable]: 'Niet beschikbaar'
	};

	onMount(async () => {
		try {
			groups = await apiClient.mine();
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	onMount(() => {
		breadcrumbs.set([{ label: 'Outing Planner' }]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Outing Planner</h1>
	<a
		href={resolve('/tools/outing-planner/new')}
		class="text-sm font-medium text-primary-hover hover:underline"
	>
		Nieuwe outing
	</a>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Outings konden niet worden geladen.</p>
{:else if groups.length === 0}
	<p class="mt-4 text-sm text-gray-500">Geen aankomende outings.</p>
{:else}
	<div class="mt-6 space-y-8">
		{#each groups as group (group.userGroupInstanceId)}
			<div>
				<div class="flex items-center justify-between">
					<h2 class="text-lg font-semibold text-gray-900">{group.userGroupInstanceName}</h2>
					<a
						href={resolve('/tools/outing-planner/instance/[instanceId]', {
							instanceId: group.userGroupInstanceId
						})}
						class="text-sm text-primary-hover hover:underline"
					>
						Alle outings
					</a>
				</div>
				<div class="mt-2 divide-y divide-gray-200 border-t border-gray-200">
					{#each group.outings as outing (outing.id)}
						<a
							href={resolve('/tools/outing-planner/[id]', { id: outing.id })}
							class="flex items-center justify-between gap-4 py-3 hover:bg-gray-50"
						>
							<div>
								<p class="text-sm font-medium text-gray-900">
									{outing.outingDate.toLocaleString('nl-NL', {
										dateStyle: 'medium',
										timeStyle: 'short'
									})}
								</p>
								<p class="text-sm text-gray-500">
									{outing.boatTypeName ?? outing.boatTypeDifferent ?? '—'}
									{#if outing.rowerCapacity}
										· Roeiers {outing.rowerCount}/{outing.rowerCapacity}
									{/if}
								</p>
							</div>
							<span class="text-sm text-gray-600">
								{outing.myRole !== undefined ? roleLabels[outing.myRole] : 'Geen status'}
							</span>
						</a>
					{/each}
				</div>
			</div>
		{/each}
	</div>
{/if}
