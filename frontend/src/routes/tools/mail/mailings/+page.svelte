<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { DataTable, Button, ConfirmDialog, breadcrumbs } from '$lib';
	import { MailingStatus } from '$lib/api/apiClient';
	import type { MailingDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let items = $state<MailingDto[]>([]);
	let error = $state(false);
	let loading = $state(true);
	let deleteDialog = $state<HTMLDialogElement>();
	let pendingDelete = $state<MailingDto>();

	async function loadItems() {
		items = await apiClient.mailingsAll();
	}

	async function handleDelete() {
		if (!pendingDelete) return;
		await apiClient.mailingsDELETE(pendingDelete.id);
		await loadItems();
	}

	$effect(() => {
		breadcrumbs.set([{ label: 'Mail', href: '/tools/mail' }, { label: 'Mailings' }]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			await loadItems();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	function statusLabel(status: MailingStatus): string {
		switch (status) {
			case MailingStatus.Draft:
				return 'Concept';
			case MailingStatus.Sending:
				return 'Wordt verzonden';
			case MailingStatus.Sent:
				return 'Verzonden';
		}
	}

	function statusClass(status: MailingStatus): string {
		switch (status) {
			case MailingStatus.Draft:
				return 'bg-gray-100 text-gray-700';
			case MailingStatus.Sending:
				return 'bg-yellow-100 text-yellow-700';
			case MailingStatus.Sent:
				return 'bg-green-100 text-green-700';
		}
	}
</script>

<div class="flex items-center justify-between">
	<h1 class="text-2xl font-bold text-gray-900">Mailings</h1>
	<Button variant="primary" size="sm" href={resolve('/tools/mail/mailings/new')}>
		Nieuwe mailing
	</Button>
</div>

{#snippet subjectCell(item: MailingDto)}
	<span class="font-medium text-gray-900">{item.subject}</span>
{/snippet}

{#snippet statusCell(item: MailingDto)}
	<span class="rounded-full px-2 py-0.5 text-xs font-medium {statusClass(item.status)}">
		{statusLabel(item.status)}
	</span>
{/snippet}

{#snippet sentAtCell(item: MailingDto)}
	<span class="text-gray-500"
		>{item.sentAt ? new Date(item.sentAt).toLocaleString('nl-NL') : '—'}</span
	>
{/snippet}

{#snippet actionsCell(item: MailingDto)}
	{#if item.status === MailingStatus.Draft}
		<button
			type="button"
			class="text-sm text-red-600 hover:underline"
			onclick={(e) => {
				e.stopPropagation();
				pendingDelete = item;
				deleteDialog?.showModal();
			}}
		>
			Verwijderen
		</button>
	{/if}
{/snippet}

<DataTable
	columns={[
		{ header: 'Onderwerp', cell: subjectCell },
		{ header: 'Status', cell: statusCell },
		{ header: 'Verzonden op', cell: sentAtCell },
		{ header: '', cell: actionsCell }
	]}
	{items}
	{loading}
	error={error ? 'Mailings konden niet worden geladen.' : undefined}
	emptyMessage="Geen mailings gevonden."
	rowHref={(item) => resolve('/tools/mail/mailings/[id]', { id: item.id })}
/>

<ConfirmDialog
	bind:dialog={deleteDialog}
	message="Weet je zeker dat je '{pendingDelete?.subject}' wil verwijderen?"
	confirmLabel="Verwijderen"
	onConfirm={handleDelete}
/>
