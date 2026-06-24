<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, Spinner, breadcrumbs } from '$lib';
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

	function handleAddGroupDialogClick(event: MouseEvent) {
		if (event.target === addGroupDialog) addGroupDialog?.close();
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
				<button
					type="button"
					onclick={() => addGroupDialog?.showModal()}
					class="rounded-md border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
				>
					Groep toevoegen
				</button>
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
	<div class="mt-6 space-y-6">
		{#each categoryOverviews as category (category.id)}
			<div class="rounded-md border border-gray-200">
				<div
					class="flex items-center justify-between border-b border-gray-200 bg-gray-50 px-4 py-2"
				>
					<span class="font-medium text-gray-900">{category.name}</span>
					<span class="text-sm text-gray-500">{category.groupCount} groepen</span>
				</div>
				<div class="divide-y divide-gray-100">
					{#each category.groups as group (group.id)}
						<a
							href={groupHref(group.id, group.instanceId)}
							class="flex items-center justify-between px-4 py-2 hover:bg-gray-50"
						>
							<span class="text-gray-900">{group.name}</span>
							{#if group.memberCount !== null && group.permissionCount !== null}
								<span class="text-sm text-gray-500">
									{group.memberCount} leden &middot; {group.permissionCount} permissies
								</span>
							{/if}
						</a>
					{:else}
						<p class="px-4 py-4 text-sm text-gray-500">Geen groepen in deze categorie.</p>
					{/each}
				</div>
			</div>
		{:else}
			<p class="text-sm text-gray-500">Geen categorieën gevonden.</p>
		{/each}
	</div>
{:else}
	<div class="mt-6 divide-y divide-gray-200 border-t border-gray-200">
		{#each categories as category (category.id)}
			<a
				href={resolve('/tools/groups/categories/[categoryId]/edit', { categoryId: category.id })}
				class="flex items-center justify-between py-2 hover:bg-gray-50"
			>
				<div>
					<p class="font-medium text-gray-900">{category.name}</p>
					<p class="text-sm text-gray-500">{category.description}</p>
				</div>
			</a>
		{:else}
			<p class="py-6 text-sm text-gray-500">Geen categorieën gevonden.</p>
		{/each}
	</div>
{/if}

<dialog
	bind:this={addGroupDialog}
	onclick={handleAddGroupDialogClick}
	class="m-auto w-full max-w-2xl rounded-md border border-gray-200 bg-white p-0 shadow-lg backdrop:bg-black/40"
>
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
			<!-- eslint-disable svelte/no-navigation-without-resolve -- resolve() result with an appended query string, not a static route literal -->
			<a
				href={resolve('/tools/groups/instances/new') +
					(selectedSeasonId ? `?seasonId=${selectedSeasonId}` : '')}
				onclick={() => addGroupDialog?.close()}
				class="mt-4 inline-block rounded-md bg-primary px-3 py-1.5 text-sm font-medium text-primary-content hover:bg-primary-hover"
			>
				<!-- eslint-enable svelte/no-navigation-without-resolve -->
				Instantie aanmaken
			</a>
		</div>
		<div class="rounded-md border border-gray-200 p-4">
			<h3 class="font-semibold text-gray-900">Nieuwe groep</h3>
			<p class="mt-2 text-sm text-gray-600">
				Maakt een volledig <strong>nieuwe</strong> groep aan, met een nieuwe naam. Gebruik dit alleen
				als de groep nog niet bestaat — voor een bestaande groep in een ander seizoen kies je een nieuwe
				instantie.
			</p>
			<a
				href={resolve('/tools/groups/groups/new')}
				onclick={() => addGroupDialog?.close()}
				class="mt-4 inline-block rounded-md border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
			>
				Groep aanmaken
			</a>
		</div>
	</div>
</dialog>
