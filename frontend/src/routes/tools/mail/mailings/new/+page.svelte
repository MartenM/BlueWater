<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { MailingForm, Spinner, breadcrumbs } from '$lib';
	import { MailTemplateKind } from '$lib/api/apiClient';
	import type {
		MailLayoutDto,
		MailSenderInfoDto,
		MailTemplateDto,
		UpsertMailingRequest
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	let senders = $state<MailSenderInfoDto[]>([]);
	let templates = $state<MailTemplateDto[]>([]);
	let layouts = $state<MailLayoutDto[]>([]);
	let loading = $state(true);
	let error = $state(false);

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Mail', href: '/tools/mail' },
			{ label: 'Mailings', href: '/tools/mail/mailings' },
			{ label: 'Nieuw' }
		]);
		return () => breadcrumbs.clear();
	});

	onMount(async () => {
		try {
			[senders, templates, layouts] = await Promise.all([
				apiClient.senders(),
				apiClient.mailTemplatesAll(MailTemplateKind.Mailing),
				apiClient.mailLayoutsAll()
			]);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	async function handleCreate(request: UpsertMailingRequest) {
		const created = await apiClient.mailingsPOST(request);
		goto(resolve('/tools/mail/mailings/[id]', { id: created.id }));
	}
</script>

<h1 class="text-2xl font-bold text-gray-900">Nieuwe mailing</h1>

{#if loading}
	<Spinner />
{:else if error}
	<p class="text-sm text-gray-600">Gegevens konden niet worden geladen.</p>
{:else}
	<div class="mt-6">
		<MailingForm {senders} {templates} {layouts} submitLabel="Aanmaken" onSubmit={handleCreate} />
	</div>
{/if}
