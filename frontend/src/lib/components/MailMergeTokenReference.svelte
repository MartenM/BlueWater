<script lang="ts">
	import type { MailPlaceholderDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	// `templateId` scopes the result to a specific MailTemplate: the backend always includes the
	// base tokens, plus any extra placeholders registered for that specific Transactional
	// template (see RequiredTransactionalMailTemplates on the backend). Omit it (e.g. for the
	// Mailing composer, or a not-yet-saved template) to get the base tokens only.
	let { templateId }: { templateId?: string } = $props();

	let placeholders = $state<MailPlaceholderDto[]>([]);
	let error = $state(false);

	$effect(() => {
		apiClient
			.placeholders(templateId)
			.then((result) => {
				placeholders = result;
				error = false;
			})
			.catch(() => {
				error = true;
			});
	});
</script>

<details class="rounded-md border border-gray-200 bg-gray-50 p-3 text-sm">
	<summary class="cursor-pointer font-medium text-gray-700">Beschikbare plaatsvariabelen</summary>
	{#if error}
		<p class="mt-2 text-sm text-red-600">Plaatsvariabelen konden niet worden geladen.</p>
	{:else}
		<ul class="mt-2 space-y-1">
			{#each placeholders as placeholder (placeholder.token)}
				<li>
					<code class="rounded bg-gray-200 px-1 py-0.5 text-xs"
						>{'{{' + placeholder.token + '}}'}</code
					>
					<span class="text-gray-600">— {placeholder.description}</span>
				</li>
			{/each}
		</ul>
	{/if}
</details>
