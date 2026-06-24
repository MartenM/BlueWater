<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { apiClient } from '$lib/api/client';
	import { CategoryForm, BlueAlert, Spinner, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import type { UpsertUserGroupCategoryRequest, UserGroupCategoryDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let category = $state<UserGroupCategoryDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);

	onMount(async () => {
		try {
			category = await apiClient.userGroupCategoriesGET(params.categoryId);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleEdit(request: UpsertUserGroupCategoryRequest) {
		category = await apiClient.userGroupCategoriesPUT(params.categoryId, request);
	}

	async function handleDelete() {
		if (!category || !confirm(`Categorie "${category.name}" verwijderen?`)) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.userGroupCategoriesDELETE(category.id);
			// eslint-disable-next-line svelte/no-navigation-without-resolve -- static route with a query string, not a literal resolve() can check
			goto('/tools/groups?tab=categories');
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Groepen', href: '/tools/groups' },
			{ label: 'Categorieën', href: '/tools/groups?tab=categories' },
			{ label: 'Categorie bewerken' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Categorie bewerken</h1>

{#if loading}
	<Spinner />
{:else if error}
	<p class="mt-4 text-sm text-gray-600">Categorie kon niet worden geladen.</p>
{:else if category}
	<div class="mt-6 max-w-md">
		<CategoryForm {category} submitLabel="Opslaan" onSubmit={handleEdit} />
	</div>

	<div class="mt-8 border-t border-gray-200 pt-6">
		<button
			type="button"
			onclick={handleDelete}
			disabled={deleting}
			class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
		>
			Categorie verwijderen
		</button>
		{#if deleteError}
			<div class="mt-3">
				<BlueAlert level={AlertLevel.Danger}>{deleteError}</BlueAlert>
			</div>
		{/if}
	</div>
{/if}
