<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import {
		HasPermission,
		BlueAlert,
		ConfirmDialog,
		Button,
		FormField,
		MemberPicker,
		Modal,
		Spinner,
		breadcrumbs
	} from '$lib';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import { BluePermission, AssignExamRequest } from '$lib/api/apiClient';
	import type { ExamTypeDto, UserExamDto } from '$lib/api/apiClient';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let examType = $state<ExamTypeDto | null>(null);
	let assigned = $state<UserExamDto[]>([]);
	let error = $state(false);
	let loading = $state(true);
	let deleteError = $state<string | null>(null);
	let deleting = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();
	let assignDialog = $state<HTMLDialogElement>();

	let assignUserId = $state('');
	let assignObtainedAt = $state('');
	const assignForm = new FormState();

	onMount(async () => {
		try {
			[examType, assigned] = await Promise.all([
				apiClient.typesGET(params.id),
				apiClient.assigned(params.id)
			]);
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (!examType) return;
		breadcrumbs.set([{ label: 'Examens', href: '/tools/exams' }, { label: examType.name }]);
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

	function openAssignDialog() {
		assignUserId = '';
		assignObtainedAt = '';
		assignDialog?.showModal();
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
			assigned = [...assigned, result];
			assignDialog?.close();
		});
	}

	async function handleUnassign(userExamId: string) {
		try {
			await apiClient.assignDELETE(userExamId);
			assigned = assigned.filter((a) => a.id !== userExamId);
		} catch {
			// not critical
		}
	}

	const dateFormatter = new Intl.DateTimeFormat('nl-NL', {
		day: 'numeric',
		month: 'long',
		year: 'numeric'
	});
</script>

{#if loading}
	<Spinner />
{:else if error}
	<p class="text-sm text-gray-600">Examentype kon niet worden geladen.</p>
{:else if examType}
	<div class="flex items-start justify-between gap-4">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{examType.name}</h1>
			{#if examType.description}
				<p class="mt-1 text-sm text-gray-600">{examType.description}</p>
			{/if}
		</div>
		<HasPermission permission={BluePermission.ExamsAssign}>
			<Button onclick={openAssignDialog}>Lid toevoegen</Button>
		</HasPermission>
	</div>

	<section class="mt-8">
		<h2 class="text-lg font-semibold text-gray-900">Behaald door</h2>

		{#if assigned.length === 0}
			<p class="mt-4 text-sm text-gray-500">Nog niemand heeft dit examen behaald.</p>
		{:else}
			<table class="mt-4 w-full text-sm">
				<thead>
					<tr
						class="border-b border-gray-200 text-left text-xs font-semibold uppercase tracking-wide text-gray-500"
					>
						<th class="pb-2 pr-4">Naam</th>
						<th class="pb-2 pr-4">Behaald op</th>
						<HasPermission permission={BluePermission.ExamsAssign}>
							<th class="pb-2"></th>
						</HasPermission>
					</tr>
				</thead>
				<tbody class="divide-y divide-gray-100">
					{#each assigned as entry (entry.id)}
						<tr>
							<td class="py-2 pr-4 font-medium text-gray-900">{entry.userFullname}</td>
							<td class="py-2 pr-4 text-gray-500">{dateFormatter.format(entry.obtainedAt)}</td>
							<HasPermission permission={BluePermission.ExamsAssign}>
								<td class="py-2 text-right">
									<button
										type="button"
										onclick={() => handleUnassign(entry.id)}
										class="font-medium text-red-600 hover:underline"
									>
										Verwijderen
									</button>
								</td>
							</HasPermission>
						</tr>
					{/each}
				</tbody>
			</table>
		{/if}
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

	<HasPermission permission={BluePermission.ExamsAssign}>
		<Modal bind:dialog={assignDialog} maxWidth="max-w-md">
			<div class="p-6">
				<h2 class="mb-4 text-lg font-semibold text-gray-900">Examen toewijzen</h2>
				<form class="flex flex-col gap-4" onsubmit={handleAssignSubmit}>
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

					<div class="flex justify-end gap-3">
						<button
							type="button"
							onclick={() => assignDialog?.close()}
							class="text-sm font-medium text-gray-600 hover:underline"
						>
							Annuleren
						</button>
						<Button type="submit" disabled={assignForm.submitting}>Toewijzen</Button>
					</div>
				</form>
			</div>
		</Modal>
	</HasPermission>
{/if}
