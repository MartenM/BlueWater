<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { ConfirmDialog, Button, Spinner, MailTemplateForm, breadcrumbs } from '$lib';
	import { MailTemplateKind } from '$lib/api/apiClient';
	import type {
		MailLayoutDto,
		MailSenderInfoDto,
		MailTemplateDto,
		UpsertMailTemplateRequest
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let template = $state<MailTemplateDto | null>(null);
	let layouts = $state<MailLayoutDto[]>([]);
	let senders = $state<MailSenderInfoDto[]>([]);
	let loading = $state(true);
	let error = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	onMount(async () => {
		try {
			[template, layouts, senders] = await Promise.all([
				apiClient.mailTemplatesGET(params.id),
				apiClient.mailLayoutsAll(),
				apiClient.senders()
			]);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (template) {
			breadcrumbs.set([
				{ label: 'Mail', href: '/tools/mail' },
				{ label: "Sjabloon's", href: '/tools/mail/templates' },
				{ label: template.name }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleEdit(request: UpsertMailTemplateRequest) {
		template = await apiClient.mailTemplatesPUT(params.id, request);
	}

	async function handleDelete() {
		await apiClient.mailTemplatesDELETE(params.id);
		goto(resolve('/tools/mail/templates'));
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !template}
	<p class="text-sm text-gray-600">Sjabloon kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<h1 class="text-2xl font-bold text-gray-900">{template.name}</h1>
		{#if template.kind === MailTemplateKind.Mailing}
			<Button variant="danger" size="sm" onclick={() => deleteDialog?.showModal()}>
				Verwijderen
			</Button>
		{:else}
			<p class="text-xs text-gray-400">Transactionele sjablonen kunnen niet worden verwijderd.</p>
		{/if}
	</div>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{template.name}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>

	<div class="mt-6">
		<MailTemplateForm {template} {layouts} {senders} submitLabel="Opslaan" onSubmit={handleEdit} />
	</div>
{/if}
