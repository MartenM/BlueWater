<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { BlueAlert, FormField, Spinner, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { UpsertUserGroupRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { UserGroupCategoryDto, UserGroupDto } from '$lib/api/apiClient';

	let categories = $state<UserGroupCategoryDto[]>([]);
	let categoriesLoading = $state(true);
	let categoriesError = $state(false);

	let categoryId = $state('');
	let name = $state('');
	let description = $state('');
	const form = new FormState();

	let nameMatches = $state<UserGroupDto[]>([]);
	let nameCheckTimer: ReturnType<typeof setTimeout> | undefined;

	onMount(async () => {
		try {
			categories = await apiClient.userGroupCategoriesAll();
			categoryId = categories[0]?.id ?? '';
		} catch {
			categoriesError = true;
		} finally {
			categoriesLoading = false;
		}
	});

	function handleNameInput() {
		clearTimeout(nameCheckTimer);
		if (name.trim().length < 2) {
			nameMatches = [];
			return;
		}
		nameCheckTimer = setTimeout(async () => {
			try {
				nameMatches = await apiClient.byName(name.trim());
			} catch {
				nameMatches = [];
			}
		}, 300);
	}

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(async () => {
			const group = await apiClient.userGroupsPOST(
				new UpsertUserGroupRequest({ name, description, userGroupCategoryId: categoryId })
			);
			goto(resolve('/tools/groups/group/[groupId]', { groupId: group.id }));
		});
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Groepen', href: '/tools/groups' }, { label: 'Nieuwe groep' }]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe groep</h1>

<div class="mt-6 max-w-md">
	{#if categoriesLoading}
		<Spinner />
	{:else if categoriesError}
		<p class="text-sm text-gray-600">Categorieën konden niet worden geladen.</p>
	{:else}
		<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
			<FormField label="Categorie" errors={form.errorsFor('userGroupCategoryId')}>
				{#snippet children(invalid)}
					<select
						bind:value={categoryId}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					>
						{#each categories as category (category.id)}
							<option value={category.id}>{category.name}</option>
						{/each}
					</select>
				{/snippet}
			</FormField>

			<FormField label="Naam" errors={form.errorsFor('name')}>
				{#snippet children(invalid)}
					<input
						type="text"
						required
						bind:value={name}
						oninput={handleNameInput}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			{#if nameMatches.length > 0}
				<p class="text-sm text-amber-700">
					Een groep met deze naam bestaat al ({nameMatches
						.map((m) => m.userGroupCategoryName)
						.join(', ')}). Wil je in plaats daarvan een
					<!-- eslint-disable svelte/no-navigation-without-resolve -- resolve() result with an appended query string, not a static route literal -->
					<a
						href={resolve('/tools/groups/instances/new') + `?groupId=${nameMatches[0].id}`}
						class="font-medium underline"
					>
						<!-- eslint-enable svelte/no-navigation-without-resolve -->
						nieuwe instantie
					</a>
					aanmaken?
				</p>
			{/if}

			<FormField label="Omschrijving" errors={form.errorsFor('description')}>
				{#snippet children(invalid)}
					<input
						type="text"
						bind:value={description}
						class="rounded-md focus:border-primary focus:ring-primary {invalid
							? 'border-red-400'
							: 'border-gray-300'}"
					/>
				{/snippet}
			</FormField>

			{#if form.formError}
				<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
			{/if}

			<button
				type="submit"
				disabled={form.submitting}
				class="mt-2 self-start rounded-md bg-primary px-4 py-2 font-medium text-primary-content hover:bg-primary-hover disabled:opacity-60"
			>
				Aanmaken
			</button>
		</form>
	{/if}
</div>
