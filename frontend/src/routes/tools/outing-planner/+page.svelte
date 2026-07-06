<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { Button, DataTable, Spinner, breadcrumbs } from '$lib';
	import {
		OutingParticipantRole,
		type OutingListItemDto,
		type OutingOverviewGroupDto,
		type OutingHistorySeasonGroupDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let groups = $state<OutingOverviewGroupDto[]>([]);
	let error = $state(false);
	let loading = $state(true);

	let history = $state<OutingHistorySeasonGroupDto[]>([]);
	let historyError = $state(false);
	let historyLoading = $state(true);

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

	onMount(async () => {
		try {
			history = await apiClient.instanceHistory();
			historyError = false;
		} catch {
			historyError = true;
		} finally {
			historyLoading = false;
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
		class="font-medium text-primary hover:underline"
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

{#if historyLoading}
	<Spinner />
{:else if historyError}
	<p class="mt-4 text-sm text-gray-600">Teams konden niet worden geladen.</p>
{:else if history.length === 0}
	<p class="mt-4 text-sm text-gray-500">Geen teams gevonden.</p>
{:else}
	<div class="mt-4 grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3">
		{#each history as season (season.seasonId)}
			<div>
				<h3 class="text-sm font-semibold text-gray-500">{season.seasonName}</h3>
				<ul class="mt-2 divide-y divide-gray-200 rounded-md border border-gray-200">
					{#each season.instances as instance (instance.id)}
						<li>
							<a
								href={resolve('/tools/outing-planner/instance/[instanceId]', {
									instanceId: instance.id
								})}
								class="block px-3 py-2 text-sm text-primary hover:bg-gray-50 hover:underline"
							>
								{instance.name}
							</a>
						</li>
					{/each}
				</ul>
			</div>
		{/each}
	</div>
{/if}
