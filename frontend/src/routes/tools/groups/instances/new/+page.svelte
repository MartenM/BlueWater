<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { BlueAlert, Button, FormField, Spinner, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { CreateUserGroupInstanceRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { SeasonDto, UserGroupDto, UserGroupInstanceDto } from '$lib/api/apiClient';

	let groups = $state<UserGroupDto[]>([]);
	let seasons = $state<SeasonDto[]>([]);
	let instances = $state<UserGroupInstanceDto[]>([]);
	let loading = $state(true);
	let loadError = $state(false);

	let groupId = $state(untrack(() => page.url.searchParams.get('groupId') ?? ''));
	let seasonId = $state(untrack(() => page.url.searchParams.get('seasonId') ?? ''));
	const form = new FormState();

	const availableSeasons = $derived(
		seasons.filter((s) => !instances.some((i) => i.userGroupId === groupId && i.seasonId === s.id))
	);

	onMount(async () => {
		try {
			[groups, seasons, instances] = await Promise.all([
				apiClient.userGroupsAll(),
				apiClient.seasonsAll(),
				apiClient.userGroupInstancesAll()
			]);
			if (!groupId) groupId = groups[0]?.id ?? '';
			if (!seasonId) seasonId = seasons.find((s) => s.isCurrent)?.id ?? seasons[0]?.id ?? '';
		} catch {
			loadError = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (!availableSeasons.some((s) => s.id === seasonId)) {
			seasonId = availableSeasons[0]?.id ?? '';
		}
	});

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(async () => {
			const instance = await apiClient.userGroupInstancesPOST(
				new CreateUserGroupInstanceRequest({ userGroupId: groupId, seasonId })
			);
			goto(resolve('/tools/groups/instance/[instanceId]', { instanceId: instance.id }));
		});
	}

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Groepen', href: '/tools/groups' },
			{ label: 'Nieuwe groep-instantie' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe groep-instantie</h1>

<div class="mt-6 max-w-md">
	{#if loading}
		<Spinner />
	{:else if loadError}
		<p class="text-sm text-gray-600">Gegevens konden niet worden geladen.</p>
	{:else if groups.length === 0}
		<p class="text-sm text-gray-600">
			Er zijn nog geen groepen om een instantie voor aan te maken.
		</p>
	{:else}
		<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
			<FormField label="Groep" errors={form.errorsFor('userGroupId')}>
				{#snippet children(invalid)}
					<select
						bind:value={groupId}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					>
						{#each groups as group (group.id)}
							<option value={group.id}>{group.name} ({group.userGroupCategoryName})</option>
						{/each}
					</select>
				{/snippet}
			</FormField>

			<FormField label="Seizoen" errors={form.errorsFor('seasonId')}>
				{#snippet children(invalid)}
					<select
						bind:value={seasonId}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					>
						{#each availableSeasons as season (season.id)}
							<option value={season.id}>{season.name}{season.isCurrent ? ' (huidig)' : ''}</option>
						{/each}
					</select>
				{/snippet}
			</FormField>

			{#if availableSeasons.length === 0}
				<p class="text-sm text-gray-600">
					Deze groep heeft al een instantie in elk beschikbaar seizoen.
				</p>
			{/if}

			{#if form.formError}
				<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
			{/if}

			<div class="mt-2 self-start">
				<Button type="submit" disabled={form.submitting || availableSeasons.length === 0}>
					Aanmaken
				</Button>
			</div>
		</form>
	{/if}
</div>
