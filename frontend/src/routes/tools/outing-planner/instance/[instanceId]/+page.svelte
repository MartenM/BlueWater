<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { Button, DataTable, Pagination, OutingPlannerTeamList, breadcrumbs } from '$lib';
	import {
		OutingParticipantRole,
		OutingView,
		type OutingListItemDto,
		type UserGroupInstanceDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	const tabs: { view: OutingView; label: string }[] = [
		{ view: OutingView.Upcoming, label: 'Aankomend' },
		{ view: OutingView.AwaitingConfirmation, label: 'Te bevestigen' },
		{ view: OutingView.RowedHistory, label: 'Geroeid' }
	];

	let activeView = $state<OutingView>(OutingView.Upcoming);
	let items = $state<OutingListItemDto[]>([]);
	let totalCount = $state(0);
	let page = $state(1);
	const pageSize = 25;
	let loading = $state(true);
	let error = $state(false);
	let instance = $state<UserGroupInstanceDto | null>(null);

	const totalPages = $derived(Math.ceil(totalCount / pageSize));

	async function load() {
		loading = true;
		try {
			const result = await apiClient.instances(params.instanceId, activeView, page, pageSize);
			items = result.items ?? [];
			totalCount = result.totalCount;
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	async function loadInstance() {
		try {
			instance = await apiClient.userGroupInstancesGET(params.instanceId);
		} catch {
			instance = null;
		}
	}

	function selectView(view: OutingView) {
		activeView = view;
		page = 1;
		load();
	}

	function goToPage(p: number) {
		page = p;
		load();
	}

	onMount(() => {
		load();
		loadInstance();
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Outing Planner', href: '/tools/outing-planner' },
			{ label: instance ? `${instance.userGroupName} (${instance.seasonName})` : 'Team' }
		]);
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
	<div>
		<h1 class="text-2xl font-bold text-gray-900">Outings</h1>
		{#if instance}
			<p class="text-sm text-gray-500">{instance.userGroupName} · {instance.seasonName}</p>
		{/if}
	</div>
	<Button href={resolve('/tools/outing-planner/new')} size="sm">Nieuwe outing</Button>
</div>

<div class="mt-6 lg:flex lg:items-start">
	<div class="lg:w-3/4">
		<div class="flex gap-4 border-b border-gray-200">
			{#each tabs as tab (tab.view)}
				<button
					type="button"
					onclick={() => selectView(tab.view)}
					class="border-b-2 px-1 pb-2 text-sm font-medium {activeView === tab.view
						? 'border-primary text-primary-hover'
						: 'border-transparent text-gray-500 hover:text-gray-700'}"
				>
					{tab.label}
				</button>
			{/each}
		</div>

		<DataTable
			columns={[
				{ header: 'Datum', cell: dateCell },
				{ header: 'Boot', cell: boatCell },
				{ header: 'Roeiers', cell: rowersCell },
				{ header: 'Mijn rol', cell: myRoleCell }
			]}
			{items}
			{loading}
			error={error ? 'Outings konden niet worden geladen.' : undefined}
			emptyMessage="Geen outings gevonden."
		/>

		{#if !loading && !error && totalPages > 1}
			<div class="mt-4">
				<Pagination {page} {totalPages} onPageChange={goToPage} />
			</div>
		{/if}
	</div>

	<div
		class="mt-10 border-t border-gray-200 pt-6 lg:mt-0 lg:w-1/4 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8"
	>
		<OutingPlannerTeamList />
	</div>
</div>
