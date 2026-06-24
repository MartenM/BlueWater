<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { Button, HasPermission, Modal, Spinner, breadcrumbs } from '$lib';
	import {
		BluePermission,
		type SeasonDto,
		type UserGroupCategoryDto,
		type UserGroupCategoryOverviewDto
	} from '$lib/api/apiClient';

	const ALL_SEASONS = '';

	let activeTab = $state<'groups' | 'categories'>(
		untrack(() => (page.url.searchParams.get('tab') === 'categories' ? 'categories' : 'groups'))
	);

	let seasons = $state<SeasonDto[]>([]);
	let selectedSeasonId = $state(ALL_SEASONS);
	let categoryOverviews = $state<UserGroupCategoryOverviewDto[]>([]);
	let categories = $state<UserGroupCategoryDto[]>([]);
	let loading = $state(true);
	let error = $state(false);

	let addGroupDialog: HTMLDialogElement | undefined = $state();

	async function loadOverview(seasonId: string) {
		try {
			categoryOverviews = await apiClient.overview(seasonId || undefined);
			error = false;
		} catch {
			error = true;
		}
	}

	onMount(async () => {
		try {
			[seasons, categories] = await Promise.all([
				apiClient.seasonsAll(),
				apiClient.userGroupCategoriesAll()
			]);
			selectedSeasonId = seasons.find((s) => s.isCurrent)?.id ?? ALL_SEASONS;
			await loadOverview(selectedSeasonId);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	function handleSeasonChange(event: Event) {
		const seasonId = (event.currentTarget as HTMLSelectElement).value;
		selectedSeasonId = seasonId;
		loadOverview(seasonId);
	}

	function groupHref(groupId: string, instanceId: string | undefined) {
		return instanceId
			? resolve('/tools/groups/instance/[instanceId]', { instanceId })
			: resolve('/tools/groups/group/[groupId]', { groupId });
	}

	$effect(() => {
		untrack(() => breadcrumbs.set([{ label: 'Groepen' }]));
		return () => breadcrumbs.clear();
	});
</script>

<div class="flex items-center justify-between">
	<div>
		<h1 class="text-2xl font-bold text-gray-900">Groepen</h1>
		<p class="text-sm text-gray-500">Overzicht van groepen per categorie, per seizoen.</p>
	</div>
	{#if activeTab === 'groups'}
		<div class="flex items-center gap-4">
			{#if seasons.length > 0}
				<select
					value={selectedSeasonId}
					onchange={handleSeasonChange}
					class="rounded-md border-gray-300 text-sm focus:border-primary focus:ring-primary"
				>
					<option value={ALL_SEASONS}>Alle seizoenen</option>
					{#each seasons as season (season.id)}
						<option value={season.id}>{season.name}{season.isCurrent ? ' (huidig)' : ''}</option>
					{/each}
				</select>
			{/if}
			<HasPermission permission={BluePermission.AdminModifyGroups}>
				<Button variant="secondary" size="sm" onclick={() => addGroupDialog?.showModal()}>
					Groep toevoegen
				</Button>
			</HasPermission>
		</div>
	{:else}
		<HasPermission permission={BluePermission.AdminModifyGroups}>
			<a
				href={resolve('/tools/groups/categories/new')}
				class="text-sm font-medium text-primary-hover hover:underline"
			>
				Nieuwe categorie
			</a>
		</HasPermission>
	{/if}
</div>

<div class="mt-4 flex gap-4 border-b border-gray-200">
	<button
		type="button"
		onclick={() => (activeTab = 'groups')}
		class="-mb-px border-b-2 px-1 py-2 text-sm font-medium {activeTab === 'groups'
			? 'border-primary text-primary-hover'
			: 'border-transparent text-gray-500 hover:text-gray-700'}"
	>
		Groepen
	</button>
	<button
		type="button"
		onclick={() => (activeTab = 'categories')}
		class="-mb-px border-b-2 px-1 py-2 text-sm font-medium {activeTab === 'categories'
			? 'border-primary text-primary-hover'
			: 'border-transparent text-gray-500 hover:text-gray-700'}"
	>
		Categorieën
	</button>
</div>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Gegevens konden niet worden geladen.</p>
{:else if activeTab === 'groups'}
	<div class="mt-4 overflow-hidden rounded-md border border-gray-200">
		<div class="grid grid-cols-[1fr_auto_auto] text-sm">
			<span
				class="border-b border-gray-200 bg-gray-50 px-3 py-1.5 text-xs font-medium text-gray-500"
			>
				Groep
			</span>
			<span
				class="border-b border-gray-200 bg-gray-50 px-3 py-1.5 text-right text-xs font-medium text-gray-500"
			>
				Leden
			</span>
			<span
				class="border-b border-gray-200 bg-gray-50 px-3 py-1.5 text-right text-xs font-medium text-gray-500"
			>
				Permissies
			</span>
			{#each categoryOverviews as category (category.id)}
				<span
					class="col-span-3 border-b border-gray-200 bg-gray-50 px-3 py-1 text-xs font-semibold text-gray-700"
				>
					{category.name}
				</span>
				{#each category.groups as group, i (group.id)}
					<a href={groupHref(group.id, group.instanceId)} class="group contents">
						<span
							class="border-b border-gray-100 py-1 pr-3 pl-6 text-gray-900 group-hover:bg-gray-50"
						>
							<span class="mr-1.5 font-mono text-gray-300">
								{i === category.groups.length - 1 ? '└' : '├'}
							</span>{group.name}
						</span>
						<span
							class="border-b border-gray-100 px-3 py-1 text-right text-gray-500 group-hover:bg-gray-50"
						>
							{group.memberCount ?? '—'}
						</span>
						<span
							class="border-b border-gray-100 px-3 py-1 text-right text-gray-500 group-hover:bg-gray-50"
						>
							{group.permissionCount ?? '—'}
						</span>
					</a>
				{:else}
					<span class="col-span-3 border-b border-gray-100 px-3 py-2 text-gray-500">
						Geen groepen in deze categorie.
					</span>
				{/each}
			{:else}
				<span class="col-span-3 px-3 py-6 text-gray-500">Geen categorieën gevonden.</span>
			{/each}
		</div>
	</div>
{:else}
	<div class="mt-4 overflow-hidden rounded-md border border-gray-200">
		<div class="grid grid-cols-[1fr_2fr] text-sm">
			<span
				class="border-b border-gray-200 bg-gray-50 px-3 py-1.5 text-xs font-medium text-gray-500"
			>
				Categorie
			</span>
			<span
				class="border-b border-gray-200 bg-gray-50 px-3 py-1.5 text-xs font-medium text-gray-500"
			>
				Omschrijving
			</span>
			{#each categories as category (category.id)}
				<a
					href={resolve('/tools/groups/categories/[categoryId]/edit', {
						categoryId: category.id
					})}
					class="group contents"
				>
					<span class="border-b border-gray-100 px-3 py-1 text-gray-900 group-hover:bg-gray-50">
						{category.name}
					</span>
					<span class="border-b border-gray-100 px-3 py-1 text-gray-500 group-hover:bg-gray-50">
						{category.description}
					</span>
				</a>
			{:else}
				<span class="col-span-2 px-3 py-6 text-gray-500">Geen categorieën gevonden.</span>
			{/each}
		</div>
	</div>
{/if}

<Modal bind:dialog={addGroupDialog} maxWidth="max-w-2xl">
	<div class="flex items-center justify-between border-b border-gray-200 px-6 py-4">
		<h2 class="text-lg font-medium text-gray-900">Groep toevoegen</h2>
		<button
			type="button"
			onclick={() => addGroupDialog?.close()}
			class="text-gray-400 hover:text-gray-600"
			aria-label="Sluiten"
		>
			&times;
		</button>
	</div>
	<div class="grid grid-cols-2 gap-6 p-6">
		<div class="rounded-md border-2 border-primary p-4">
			<h3 class="font-semibold text-gray-900">Nieuwe groep-instantie</h3>
			<p class="mt-2 text-sm text-gray-600">
				Activeer een <strong>bestaande</strong> groep voor een seizoen. Dit is wat je in bijna alle gevallen
				wilt — bijvoorbeeld als een groep dit seizoen weer moet meedraaien.
			</p>
			<div class="mt-4">
				<Button
					variant="primary"
					size="sm"
					href={resolve('/tools/groups/instances/new') +
						(selectedSeasonId ? `?seasonId=${selectedSeasonId}` : '')}
					onclick={() => addGroupDialog?.close()}
				>
					Instantie aanmaken
				</Button>
			</div>
		</div>
		<div class="rounded-md border border-gray-200 p-4">
			<h3 class="font-semibold text-gray-900">Nieuwe groep</h3>
			<p class="mt-2 text-sm text-gray-600">
				Maakt een volledig <strong>nieuwe</strong> groep aan, met een nieuwe naam. Gebruik dit alleen
				als de groep nog niet bestaat.
			</p>
			<div class="mt-4">
				<Button
					variant="secondary"
					size="sm"
					href={resolve('/tools/groups/groups/new')}
					onclick={() => addGroupDialog?.close()}
				>
					Groep aanmaken
				</Button>
			</div>
		</div>
	</div>
</Modal>
