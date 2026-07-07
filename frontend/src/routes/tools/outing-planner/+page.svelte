<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { Button, DataTable, Spinner, OutingPlannerTeamList, breadcrumbs } from '$lib';
	import {
		OutingParticipantRole,
		type OutingListItemDto,
		type OutingOverviewGroupDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let groups = $state<OutingOverviewGroupDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

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

{#snippet dateCell(item: OutingListItemDto)}
	<a
		href={resolve('/tools/outing-planner/[id]', { id: item.id })}
		class="font-medium text-primary-hover hover:underline"
	>
		{item.outingDate.toLocaleString('nl-NL', { dateStyle: 'medium', timeStyle: 'short' })}
	</a>
{/snippet}
{#snippet boatCell(item: OutingListItemDto)}
	<span class="text-gray-600">{item.boatTypeName ?? item.boatTypeDifferent ?? '—'}</span>
{/snippet}
{#snippet rowersCell(item: OutingListItemDto)}
	<span class="text-gray-600">
		{item.rowerCapacity ? `${item.rowerCount}/${item.rowerCapacity}` : item.rowerCount}
	</span>
{/snippet}
{#snippet myRoleCell(item: OutingListItemDto)}
	<span class="text-gray-600">
		{item.myRole !== undefined && item.myRole !== OutingParticipantRole.None ? item.myRole : '—'}
	</span>
{/snippet}

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Outing Planner</h1>
	<Button href={resolve('/tools/outing-planner/new')} size="sm">Nieuwe outing</Button>
</div>

<div class="mt-6 lg:flex lg:items-start">
	<div class="lg:w-3/4">
		{#if loading}
			<Spinner />
		{:else if error}
			<p class="text-sm text-gray-600">Outings konden niet worden geladen.</p>
		{:else if groups.length === 0}
			<p class="text-sm text-gray-500">Geen aankomende outings.</p>
		{:else}
			<div class="space-y-8">
				{#each groups as group (group.userGroupInstanceId)}
					<div>
						<div class="flex items-center justify-between">
							<h2 class="text-lg font-semibold text-gray-900">{group.userGroupInstanceName}</h2>
						</div>
						<DataTable
							columns={[
								{ header: 'Datum', cell: dateCell },
								{ header: 'Boot', cell: boatCell },
								{ header: 'Roeiers', cell: rowersCell },
								{ header: 'Mijn rol', cell: myRoleCell }
							]}
							items={group.outings}
							emptyMessage="Geen outings gevonden."
						/>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div
		class="mt-10 border-t border-gray-200 pt-6 lg:mt-0 lg:w-1/4 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8"
	>
		<OutingPlannerTeamList />
	</div>
</div>
