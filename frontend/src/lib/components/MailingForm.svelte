<script lang="ts">
	import { untrack } from 'svelte';
	import { UpsertMailingRequest } from '$lib/api/apiClient';
	import { FormState } from '$lib/forms/formState.svelte';
	import type {
		MailingDto,
		MailSenderInfoDto,
		MailLayoutDto,
		MailTemplateDto
	} from '$lib/api/apiClient';
	import { BlueForm } from '$lib';
	import FormField from './FormField.svelte';
	import MailMergeTokenReference from './MailMergeTokenReference.svelte';
	import MailingPreviewPanel from './MailingPreviewPanel.svelte';

	let {
		mailing,
		senders,
		templates: mailingTemplates,
		layouts,
		submitLabel,
		onSubmit
	}: {
		mailing?: MailingDto;
		senders: MailSenderInfoDto[];
		templates: MailTemplateDto[];
		layouts: MailLayoutDto[];
		submitLabel: string;
		onSubmit: (request: UpsertMailingRequest) => Promise<void>;
	} = $props();

	let subject = $state(untrack(() => mailing?.subject) ?? '');
	let bodyMarkdown = $state(untrack(() => mailing?.bodyMarkdown) ?? '');
	let senderKey = $state(untrack(() => mailing?.senderKey) ?? '');
	let templateId = $state<string | undefined>(untrack(() => mailing?.templateId));
	let layoutId = $state<string | undefined>(untrack(() => mailing?.layoutId));
	const form = new FormState();

	function onTemplateChange() {
		const template = mailingTemplates.find((t) => t.id === templateId);
		if (!template) return;
		subject = template.subjectTemplate;
		bodyMarkdown = template.bodyMarkdown;
		if (template.defaultLayoutId) layoutId = template.defaultLayoutId;
		if (template.defaultSenderKey) senderKey = template.defaultSenderKey;
	}
</script>

<div class="grid grid-cols-1 gap-8 lg:grid-cols-2">
	<BlueForm
		{form}
		{submitLabel}
		onsubmit={() =>
			onSubmit(
				new UpsertMailingRequest({
					subject,
					bodyMarkdown,
					senderKey,
					templateId,
					layoutId
				})
			)}
	>
		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Sjabloon</span>
			<select
				value={templateId ?? ''}
				onchange={(e) => {
					templateId = e.currentTarget.value || undefined;
					onTemplateChange();
				}}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			>
				<option value="">— Geen sjabloon —</option>
				{#each mailingTemplates as template (template.id)}
					<option value={template.id}>{template.name}</option>
				{/each}
			</select>
		</label>

		<FormField label="Onderwerp" errors={form.errorsFor('subject')}>
			{#snippet children(invalid)}
				<input
					type="text"
					required
					bind:value={subject}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Verzender</span>
			<select
				bind:value={senderKey}
				required
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			>
				<option value="" disabled>— Kies een verzender —</option>
				{#each senders as sender (sender.key)}
					<option value={sender.key}>{sender.displayName}</option>
				{/each}
			</select>
		</label>

		<label class="flex flex-col gap-1">
			<span class="text-sm font-medium text-gray-700">Layout</span>
			<select
				value={layoutId ?? ''}
				onchange={(e) => (layoutId = e.currentTarget.value || undefined)}
				class="rounded-md border-gray-300 focus:border-primary focus:ring-primary"
			>
				<option value="">— Geen layout —</option>
				{#each layouts as layout (layout.id)}
					<option value={layout.id}>{layout.name}</option>
				{/each}
			</select>
		</label>

		<MailMergeTokenReference {templateId} />

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
	</BlueForm>

	<MailingPreviewPanel {mailing} />
</div>
