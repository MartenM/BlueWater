<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { CategoryForm, breadcrumbs } from '$lib';
	import type { UpsertUserGroupCategoryRequest } from '$lib/api/apiClient';

	async function handleCreate(request: UpsertUserGroupCategoryRequest) {
		const category = await apiClient.userGroupCategoriesPOST(request);
		goto(resolve('/tools/groups/categories/[categoryId]/edit', { categoryId: category.id }));
	}

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Groepen', href: '/tools/groups' },
			{ label: 'Categorieën', href: '/tools/groups?tab=categories' },
			{ label: 'Nieuwe categorie' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe categorie</h1>
<div class="mt-6 max-w-md">
	<CategoryForm submitLabel="Aanmaken" onSubmit={handleCreate} />
</div>
