<script lang="ts">
	import { onMount } from 'svelte';
	import { pushState } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { DataTable, HasPermission, Pagination } from '$lib';
	import { BluePermission } from '$lib/api/apiClient';
	import type { UserDto } from '$lib/api/apiClient';

	const USERS_PAGE_SIZE = 20;

	let currentPage = $state(1);
	let search = $state('');
	let items = $state<UserDto[]>([]);
	let totalCount = $state(0);
	let error = $state(false);
	let loading = $state(true);

	const totalPages = $derived(Math.max(1, Math.ceil(totalCount / USERS_PAGE_SIZE)));

	async function reload(page: number) {
		currentPage = page;
		const query = `page=${page}${search ? `&search=${encodeURIComponent(search)}` : ''}`;
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- query-only list state, not a static route resolve() can check
		pushState(`?${query}`, {});
		try {
			const result = await apiClient.listUsers(page, USERS_PAGE_SIZE, search || undefined);
			items = result.items;
			totalCount = result.totalCount;
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	onMount(() => reload(1));

	function handleSearchSubmit(event: SubmitEvent) {
		event.preventDefault();
		reload(1);
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Gebruikers</h1>
	<HasPermission permission={BluePermission.AdminUsersModify}>
		<a
			href={resolve('/tools/users/new')}
			class="text-sm font-medium text-primary-hover hover:underline"
		>
			Nieuwe gebruiker
		</a>
	</HasPermission>
</div>

<form class="mt-6" onsubmit={handleSearchSubmit}>
	<input
		type="search"
		placeholder="Zoek op naam, gebruikersnaam of e-mailadres"
		bind:value={search}
		class="w-full rounded-md border-gray-300 focus:border-primary focus:ring-primary"
	/>
</form>

{#snippet nameCell(item: UserDto)}
	<span class="font-medium text-gray-900">{item.fullname}</span>
{/snippet}

{#snippet accountCell(item: UserDto)}
	<span class="text-sm text-gray-500">{item.userName} &middot; {item.email}</span>
{/snippet}

<DataTable
	columns={[
		{ header: 'Naam', cell: nameCell },
		{ header: 'Account', cell: accountCell }
	]}
	{items}
	{loading}
	error={error ? 'Gebruikers konden niet worden geladen.' : undefined}
	emptyMessage="Geen gebruikers gevonden."
	rowHref={(item) => resolve('/tools/users/[id]', { id: item.id })}
/>
<Pagination page={currentPage} {totalPages} onPageChange={reload} />
