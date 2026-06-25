<script lang="ts">
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { renderMarkdown } from '$lib/markdown';
	import { HasPermission, BlueAlert, ConfirmDialog, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { BluePermission } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});
	const timeFormatter = new Intl.DateTimeFormat('nl-NL', { hour: '2-digit', minute: '2-digit' });

	let item = $state(untrack(() => data.item));
	let error = $state(untrack(() => data.error));
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	function formatTime(time: string): string {
		const [hours, minutes] = time.split(':');
		return timeFormatter.format(new Date(0, 0, 0, Number(hours), Number(minutes)));
	}

	$effect(() => {
		if (!item) return;
		breadcrumbs.set([{ label: 'Agenda', href: '/agenda' }, { label: item.title }]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!item) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.agendaDELETE(item.id);
			goto(resolve('/agenda'));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-12 sm:px-6 lg:px-8">
	{#if error}
		<p class="text-sm text-gray-600">Agenda kon niet worden geladen.</p>
	{:else if item}
		<div class="text-xs font-medium text-gray-500">
			<time datetime={item.date.toISOString()}>{dateFormatter.format(item.date)}</time>
			{#if item.time}
				<span>· {formatTime(item.time)}</span>
			{/if}
			{#if item.endDate}
				<span>
					t/m
					<time datetime={item.endDate.toISOString()}>{dateFormatter.format(item.endDate)}</time>
					{#if item.endTime}
						<span>· {formatTime(item.endTime)}</span>
					{/if}
				</span>
			{:else if item.endTime}
				<span>· {formatTime(item.endTime)}</span>
			{/if}
		</div>
		<h1 class="mt-1 text-2xl font-bold text-gray-900">{item.title}</h1>

		<div class="prose prose-sm mt-6 max-w-none text-gray-700">
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- description is rendered Markdown, sanitized in $lib/markdown -->
			{@html renderMarkdown(item.description)}
		</div>

		<HasPermission permission={BluePermission.AgendaModify}>
			<div class="mt-8 flex gap-3 border-t border-gray-200 pt-6">
				<a
					href={resolve('/agenda/[id]/edit', { id: item.id })}
					class="text-sm font-medium text-primary-hover hover:underline"
				>
					Bewerken
				</a>
				<button
					type="button"
					onclick={() => deleteDialog?.showModal()}
					disabled={deleting}
					class="text-sm font-medium text-red-600 hover:underline disabled:opacity-60"
				>
					Verwijderen
				</button>
			</div>
			{#if deleteError}
				<div class="mt-4">
					<BlueAlert level={AlertLevel.Danger}>{deleteError}</BlueAlert>
				</div>
			{/if}
			<ConfirmDialog
				bind:dialog={deleteDialog}
				message="Weet je zeker dat je dit agendapunt wilt verwijderen?"
				onConfirm={handleDelete}
			/>
		</HasPermission>
	{/if}
</div>
