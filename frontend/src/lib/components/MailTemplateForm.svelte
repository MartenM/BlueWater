<script lang="ts">
	import { untrack } from 'svelte';
	import {
		UpsertMailTemplateRequest,
		MailTemplateKind,
		MailTemplatePreviewRequest
	} from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type {
		MailLayoutDto,
		MailSenderInfoDto,
		MailTemplateDto,
		MailTemplatePreviewDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { BlueForm } from '$lib';
	import FormField from './FormField.svelte';
	import MailMergeTokenReference from './MailMergeTokenReference.svelte';

	const PREVIEW_DEBOUNCE_MS = 300;

	let {
		template,
		layouts,
		senders,
		submitLabel,
		onSubmit
	}: {
		template?: MailTemplateDto;
		layouts: MailLayoutDto[];
		senders: MailSenderInfoDto[];
		submitLabel: string;
		onSubmit: (request: UpsertMailTemplateRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => template?.name) ?? '');
	const kind = untrack(() => template?.kind) ?? MailTemplateKind.Mailing;
	const isTransactional = kind === MailTemplateKind.Transactional;
	let subjectTemplate = $state(untrack(() => template?.subjectTemplate) ?? '');
	let bodyMarkdown = $state(untrack(() => template?.bodyMarkdown) ?? '');
	let defaultLayoutId = $state<string | undefined>(untrack(() => template?.defaultLayoutId));
	let defaultSenderKey = $state(untrack(() => template?.defaultSenderKey) ?? '');
	const form = new FormState();

	let preview = $state<MailTemplatePreviewDto | null>(null);
	let previewError = $state(false);
	let previewTimeout: ReturnType<typeof setTimeout> | undefined;

	$effect(() => {
		// Track the fields relevant to the preview; nothing to preview against until saved once.
		void subjectTemplate;
		void bodyMarkdown;
		void defaultLayoutId;

		if (!template) return;

		clearTimeout(previewTimeout);
		previewTimeout = setTimeout(() => {
			apiClient
				.preview(
					template!.id,
					new MailTemplatePreviewRequest({
						subjectTemplate,
						bodyMarkdown,
						layoutId: defaultLayoutId
					})
				)
				.then((result) => {
					preview = result;
					previewError = false;
				})
				.catch(() => {
					previewError = true;
				});
		}, PREVIEW_DEBOUNCE_MS);

		return () => clearTimeout(previewTimeout);
	});
</script>

<div class="grid grid-cols-1 gap-8 lg:grid-cols-2">
	<BlueForm
		{form}
		{submitLabel}
		onsubmit={() =>
			onSubmit(
				new UpsertMailTemplateRequest({
					name,
					kind,
					subjectTemplate,
					bodyMarkdown,
					defaultLayoutId,
					defaultSenderKey: defaultSenderKey || undefined
				})
			)}
	>
		<FormField label="Naam" errors={form.errorsFor('name')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					disabled={isTransactional}
					bind:value={name}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'} {isTransactional ? 'bg-gray-100 text-gray-500' : ''}"
				/>
			{/snippet}
		</FormField>
		{#if isTransactional}
			<p class="-mt-4 text-xs text-gray-400">
				De naam van een transactioneel sjabloon kan niet worden gewijzigd: deze wordt in de code
				gebruikt om het sjabloon te herkennen.
			</p>
		{/if}

		<div class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Type</span>
			<p class="text-sm text-gray-900">
				{kind === MailTemplateKind.Mailing ? 'Mailing' : 'Transactioneel'}
			</p>
		</div>

		<FormField label="Onderwerp" errors={form.errorsFor('subjectTemplate')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					bind:value={subjectTemplate}
					placeholder="Hallo {'{{FirstName}}'}"
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<MailMergeTokenReference templateId={template?.id} />

		<FormField label="Inhoud (Markdown)" errors={form.errorsFor('bodyMarkdown')}>
			{#snippet children(invalid)}
				<textarea
					required
					rows="14"
					bind:value={bodyMarkdown}
					class="font-mono text-sm rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"></textarea>
			{/snippet}
		</FormField>

		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Layout</span>
			<select
				value={defaultLayoutId ?? ''}
				onchange={(e) => (defaultLayoutId = e.currentTarget.value || undefined)}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			>
				<option value="">— Geen layout —</option>
				{#each layouts as layout (layout.id)}
					<option value={layout.id}>{layout.name}</option>
				{/each}
			</select>
		</label>

		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Standaard verzender</span>
			<select
				value={defaultSenderKey}
				onchange={(e) => (defaultSenderKey = e.currentTarget.value)}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			>
				<option value="">— Geen standaard verzender —</option>
				{#each senders as sender (sender.key)}
					<option value={sender.key}>{sender.displayName}</option>
				{/each}
			</select>
		</label>
	</BlueForm>

	<div>
		<h2 class="text-sm font-semibold text-gray-700">Voorbeeld</h2>
		{#if !template}
			<p class="mt-2 text-sm text-gray-500">Sla het sjabloon eerst op om een voorbeeld te zien.</p>
		{:else if previewError}
			<p class="mt-2 text-sm text-red-600">Voorbeeld kon niet worden geladen.</p>
		{:else if preview}
			<p class="mt-2 text-sm font-medium text-gray-900">{preview.subject}</p>
			<div class="mt-2 rounded-md border border-gray-200 bg-white p-4">
				<!-- eslint-disable-next-line svelte/no-at-html-tags -- server-rendered preview HTML, only reachable by admins with AdminModifyMailTemplates -->
				{@html preview.htmlBody}
			</div>
		{/if}
	</div>
</div>
