<script lang="ts">
	import type { MailingDto, MailingPreviewDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';

	// Takes the whole `mailing` object (not just its id) so the preview refetches whenever the
	// parent hands us a freshly-saved/reloaded mailing — including after a send, when the mailing
	// is immutable but its rendered content should still be inspectable ("what was sent").
	// `mailing` is undefined for a not-yet-created mailing (the "new" page) — the column still
	// renders so the layout doesn't jump once it becomes available, just without a preview yet.
	let { mailing }: { mailing?: MailingDto } = $props();

	let preview = $state<MailingPreviewDto | null>(null);
	let error = $state(false);

	$effect(() => {
		const id = mailing?.id;
		if (!id) {
			preview = null;
			error = false;
			return;
		}
		apiClient
			.mailingsPreview(id)
			.then((result) => {
				preview = result;
				error = false;
			})
			.catch(() => {
				error = true;
			});
	});
</script>

<div>
	<h2 class="text-sm font-semibold text-gray-700">Voorbeeld (met layout)</h2>
	<p class="mt-1 text-xs text-gray-400">
		Weergave met plaatsvariabele-vervanging (voorbeeldgegevens) en layout — dit is de inhoud die
		is/wordt verzonden, ongeacht de status van de mailing.
	</p>
	{#if !mailing}
		<p class="mt-2 text-sm text-gray-500">
			Sla de mailing eerst op om het voorbeeld met layout en voorbeeldgegevens te zien.
		</p>
	{:else if error}
		<p class="mt-2 text-sm text-red-600">Voorbeeld kon niet worden geladen.</p>
	{:else if preview}
		<p class="mt-2 text-sm font-medium text-gray-900">{preview.subject}</p>
		<div class="mt-2 rounded-md border border-gray-200 bg-white p-4">
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- server-rendered preview HTML, only reachable by admins with AdminModifyMailings -->
			{@html preview.htmlBody}
		</div>
	{/if}
</div>
