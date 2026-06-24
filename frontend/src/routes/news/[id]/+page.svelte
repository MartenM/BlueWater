<script lang="ts">
	import { onDestroy, onMount, untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { renderMarkdown } from '$lib/markdown';
	import { HasPermission, BlueAlert, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { BluePermission } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});

	let post = $state(untrack(() => data.post));
	let error = $state(untrack(() => data.error));
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);
	let iconUrl = $state<string | null>(null);

	onMount(async () => {
		if (!post?.iconId) return;
		try {
			const icon = await apiClient.content(post.iconId);
			iconUrl = URL.createObjectURL(icon.data);
		} catch {
			// Icon failed to load; the post still renders without it.
		}
	});

	onDestroy(() => {
		if (iconUrl) URL.revokeObjectURL(iconUrl);
	});

	$effect(() => {
		if (!post) return;
		breadcrumbs.set([{ label: 'Nieuws', href: '/news' }, { label: post.title }]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!post || !confirm('Weet je zeker dat je dit nieuwsbericht wilt verwijderen?')) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.newsDELETE(post.id);
			goto(resolve('/news'));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}
</script>

<div class="mx-auto max-w-3xl px-4 py-12 sm:px-6 lg:px-8">
	{#if error}
		<p class="text-sm text-gray-600">Nieuws kon niet worden geladen.</p>
	{:else if post}
		<article class="flex gap-4">
			{#if iconUrl}
				<img src={iconUrl} alt="" class="h-[100px] w-[100px] shrink-0 rounded-md object-cover" />
			{/if}
			<div>
				<time datetime={post.createdAt.toISOString()} class="text-xs font-medium text-gray-500">
					{dateFormatter.format(post.createdAt)}
				</time>
				<h1 class="mt-1 text-2xl font-bold text-gray-900">{post.title}</h1>
			</div>
		</article>

		<div class="prose prose-sm mt-6 max-w-none text-gray-700">
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- shortText/additionalText are rendered Markdown, sanitized in $lib/markdown -->
			{@html renderMarkdown(post.shortText)}
			{#if post.additionalText}
				<!-- eslint-disable-next-line svelte/no-at-html-tags -- shortText/additionalText are rendered Markdown, sanitized in $lib/markdown -->
				{@html renderMarkdown(post.additionalText)}
			{/if}
		</div>

		<HasPermission permission={BluePermission.NewsModify}>
			<div class="mt-8 flex gap-3 border-t border-gray-200 pt-6">
				<a
					href={resolve('/news/[id]/edit', { id: post.id })}
					class="text-sm font-medium text-primary-hover hover:underline"
				>
					Bewerken
				</a>
				<button
					type="button"
					onclick={handleDelete}
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
		</HasPermission>
	{/if}
</div>
