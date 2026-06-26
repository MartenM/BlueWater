<script lang="ts">
	import { untrack } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import { HasPermission, BlueAlert, ConfirmDialog, Button, FormField, MemberPicker, breadcrumbs } from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import { BluePermission, AssignExamRequest } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	let examType = $state(untrack(() => data.examType));
	let assigned = $state(untrack(() => data.assigned));
	let error = $state(untrack(() => data.error));
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();

	let assignUserId = $state('');
	let assignObtainedAt = $state('');
	const assignForm = new FormState();

	$effect(() => {
		if (!examType) return;
		breadcrumbs.set([
			{ label: 'Examentypes', href: '/tools/exams' },
			{ label: examType.name }
		]);
		return () => breadcrumbs.clear();
	});

	async function handleDelete() {
		if (!examType) return;
		deleting = true;
		deleteError = null;
		try {
			await apiClient.typesDELETE(examType.id);
			goto(resolve('/tools/exams'));
		} catch {
			deleteError = 'Verwijderen is mislukt. Probeer het later opnieuw.';
			deleting = false;
		}
	}

	function handleAssignSubmit(event: SubmitEvent) {
		event.preventDefault();
		if (!examType) return;
		assignForm.submit(async () => {
			const result = await apiClient.assignPOST(
				new AssignExamRequest({
					userId: assignUserId,
					examTypeId: examType!.id,
					obtainedAt: new Date(assignObtainedAt)
				})
			);
			assigned = [
				...assigned,
				{
					id: result.id,
					userId: result.userId,
					userFullname: result.userFullname,
					obtainedAt: result.obtainedAt
				}
			];
			assignUserId = '';
			assignObtainedAt = '';
		});
	}

	async function handleUnassign(userExamId: string) {
		try {
			await apiClient.assignDELETE(userExamId);
			assigned = assigned.filter((a) => a.id !== userExamId);
		} catch {
			// show inline error is not critical here
		}
	}

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});
</script>

{#if error}
	<p class="text-sm text-gray-600">Examentype kon niet worden geladen.</p>
{:else if examType}
	<h1 class="text-2xl font-bold text-gray-900">{examType.name}</h1>

	{#if examType.description}
		<p class="mt-2 text-sm text-gray-600">{examType.description}</p>
	{/if}

	<section class="mt-8">
		<h2 class="text-lg font-semibold text-gray-900">Behaald door</h2>

		<div class="mt-4 divide-y divide-gray-200 border-t border-gray-200">
			{#each assigned as entry (entry.id)}
				<div class="flex items-center justify-between py-3">
					<div>
						<p class="text-sm font-medium text-gray-900">{entry.userFullname}</p>
						<p class="text-xs text-gray-500">{dateFormatter.format(entry.obtainedAt)}</p>
					</div>
					<HasPermission permission={BluePermission.ExamsAssign}>
						<button
							type="button"
							onclick={() => handleUnassign(entry.id)}
							class="text-xs font-medium text-red-600 hover:underline"
						>
							Verwijderen
						</button>
					</HasPermission>
				</div>
			{:else}
				<p class="py-4 text-sm text-gray-500">Nog niemand heeft dit examen behaald.</p>
			{/each}
		</div>

		<HasPermission permission={BluePermission.ExamsAssign}>
			<form class="mt-6 flex flex-col gap-4" onsubmit={handleAssignSubmit}>
				<h3 class="text-sm font-semibold text-gray-900">Examen toewijzen</h3>

				<FormField label="Lid" errors={assignForm.errorsFor('userId')}>
					{#snippet children(invalid)}
						<MemberPicker bind:value={assignUserId} {invalid} />
					{/snippet}
				</FormField>

				<FormField label="Behaald op" errors={assignForm.errorsFor('obtainedAt')}>
					{#snippet children(invalid)}
						<input
							type="date"
							bind:value={assignObtainedAt}
							class="rounded-md focus:border-primary focus:ring-primary {invalid
								? 'border-red-400'
								: 'border-gray-300'}"
						/>
					{/snippet}
				</FormField>

				{#if assignForm.formError}
					<BlueAlert level={AlertLevel.Danger}>{assignForm.formError}</BlueAlert>
				{/if}

				<div class="self-start">
					<Button type="submit" disabled={assignForm.submitting}>Toewijzen</Button>
				</div>
			</form>
		</HasPermission>
	</section>

	<HasPermission permission={BluePermission.ExamsModify}>
		<div class="mt-8 flex gap-3 border-t border-gray-200 pt-6">
			<a
				href={resolve('/tools/exams/[id]/edit', { id: examType.id })}
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
			message={`Weet je zeker dat je het examentype '${examType.name}' wilt verwijderen?`}
			onConfirm={handleDelete}
		/>
	</HasPermission>
{/if}
