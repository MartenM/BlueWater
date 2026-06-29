<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { apiClient } from '$lib/api/client';
	import {
		BluePermission,
		SignupInputFieldType,
		SubmitResponseRequest,
		UpdateResponseRequest,
		FieldValueRequest
	} from '$lib/api/apiClient';
	import type { SignupDetailDto } from '$lib/api/apiClient';
	import { HasPermission, Button, ConfirmDialog, Modal, breadcrumbs } from '$lib';
	import { FormField } from '$lib';
	import MemberPicker from '$lib/components/MemberPicker.svelte';
	import { FormState } from '$lib/forms/formState.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	const signupId = params.id;

	let detail = $state<SignupDetailDto | null>(null);
	let loadError = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();
	let aanmeldenModal = $state<HTMLDialogElement>();
	let isEditing = $state(false);

	const responseForm = new FormState();
	const deleteForm = new FormState();

	let fieldValues = $state<Record<string, string>>({});
	let memberValues = $state<Record<string, string | null>>({});

	function initFieldStates(d: SignupDetailDto) {
		const mv: Record<string, string | null> = {};
		for (const f of d.fields) {
			if (f.type === SignupInputFieldType.OtherMember) {
				mv[f.id] = null;
			}
		}
		if (d.myResponse) {
			const vals: Record<string, string> = {};
			for (const fv of d.myResponse.fieldValues) {
				vals[fv.fieldId] = fv.value ?? '';
				if (Object.prototype.hasOwnProperty.call(mv, fv.fieldId)) {
					mv[fv.fieldId] = fv.value ?? null;
				}
			}
			fieldValues = vals;
		}
		memberValues = mv;
	}

	onMount(async () => {
		try {
			detail = await apiClient.signupsGET2(signupId);
			initFieldStates(detail);
		} catch {
			loadError = true;
		}
	});

	$effect(() => {
		if (detail) {
			breadcrumbs.set([
				{ label: 'Inschrijvingen', href: resolve('/signup') },
				{ label: detail.title }
			]);
		}
		return () => breadcrumbs.clear();
	});

	const isOpen = $derived(!detail?.endDate || new Date(detail.endDate) >= new Date());

	function formatDate(d: Date | string | undefined) {
		if (!d) return null;
		return new Date(d).toLocaleDateString('nl-NL', { day: 'numeric', month: 'long', year: 'numeric' });
	}

	function openAanmelden() {
		isEditing = false;
		aanmeldenModal?.showModal();
	}

	function openEdit() {
		isEditing = true;
		aanmeldenModal?.showModal();
	}

	async function handleSubmit() {
		if (!detail) return;
		const fvList = detail.fields.map(
			(f) => new FieldValueRequest({
				fieldId: f.id,
				value: f.type === SignupInputFieldType.OtherMember
					? memberValues[f.id] ?? undefined
					: fieldValues[f.id] ?? undefined,
			})
		);
		await responseForm.submit(async () => {
			await apiClient.responsesPOST(
				signupId,
				new SubmitResponseRequest({ fieldValues: fvList })
			);
			detail = await apiClient.signupsGET2(signupId);
			initFieldStates(detail);
			aanmeldenModal?.close();
		});
	}

	async function handleUpdate() {
		if (!detail?.myResponse) return;
		const fvList = detail.fields.map(
			(f) => new FieldValueRequest({
				fieldId: f.id,
				value: f.type === SignupInputFieldType.OtherMember
					? memberValues[f.id] ?? undefined
					: fieldValues[f.id] ?? undefined,
			})
		);
		await responseForm.submit(async () => {
			await apiClient.responsesPUT(
				signupId,
				detail!.myResponse!.id,
				new UpdateResponseRequest({ fieldValues: fvList })
			);
			detail = await apiClient.signupsGET2(signupId);
			initFieldStates(detail);
			aanmeldenModal?.close();
		});
	}

	async function handleDelete() {
		if (!detail?.myResponse) return;
		await deleteForm.submit(async () => {
			await apiClient.responsesDELETE2(signupId, detail!.myResponse!.id);
			detail = await apiClient.signupsGET2(signupId);
		});
	}

	const hasRowActions = $derived(
		!!detail?.myResponse && isOpen && (!!detail?.allowUpdate || !!detail?.allowDelete)
	);
</script>

{#if loadError}
	<p class="text-sm text-gray-600">Deze inschrijving kon niet worden geladen.</p>
{:else if !detail}
	<p class="text-sm text-gray-600">Laden…</p>
{:else}
	<div class="mx-auto max-w-3xl px-4 py-12 sm:px-6 lg:px-8 space-y-8">
		<!-- Header -->
		<div class="flex items-start justify-between gap-4">
			<div class="flex-1 min-w-0">
				<a href={resolve('/signup')} class="text-sm text-gray-500 hover:underline">← Inschrijvingen</a>
				<h1 class="mt-2 text-2xl font-bold text-gray-900">{detail.title}</h1>
				{#if detail.description}
					<p class="mt-2 text-gray-600">{detail.description}</p>
				{/if}
				<div class="mt-3 flex flex-wrap gap-4 text-sm text-gray-500">
					{#if detail.endDate}
						<span class:text-red-600={!isOpen}>
							{isOpen ? 'Inschrijven tot' : 'Gesloten op'}: {formatDate(detail.endDate)}
						</span>
					{/if}
					{#if detail.maxSignups != null}
						<span>{detail.maxSignups} plaatsen beschikbaar</span>
					{/if}
				</div>
			</div>

			<!-- Aanmelden button (top-right) -->
			<HasPermission permission={BluePermission.SignupRespond}>
				{#if isOpen && !detail.myResponse}
					<div class="shrink-0 mt-6">
						<Button variant="primary" onclick={openAanmelden}>
							Aanmelden
						</Button>
					</div>
				{/if}
			</HasPermission>
		</div>

		<!-- Response list -->
		{#if !detail.hideSignups && detail.responses != null}
			<div>
				<h2 class="text-lg font-semibold text-gray-800">Aanmeldingen</h2>
				{#if detail.responses.length === 0}
					<p class="mt-2 text-sm text-gray-500">Nog niemand aangemeld.</p>
				{:else}
					<div class="mt-2 overflow-x-auto">
						<table class="min-w-full text-sm text-left">
							<thead class="text-gray-500 border-b border-gray-200">
								<tr>
									{#if !detail.anonymous}
										<th class="py-2 pr-4 font-medium">Naam</th>
									{/if}
									<th class="py-2 pr-4 font-medium">Status</th>
									{#each detail.fields as field (field.id)}
										<th class="py-2 pr-4 font-medium">{field.title}</th>
									{/each}
									{#if hasRowActions}
										<th class="py-2 font-medium"></th>
									{/if}
								</tr>
							</thead>
							<tbody class="divide-y divide-gray-100">
								{#each detail.responses as r (r.id)}
									{@const isOwn = r.id === detail.myResponse?.id}
									<tr class:bg-blue-50={isOwn}>
										{#if !detail.anonymous}
											<td class="py-2 pr-4" class:font-medium={isOwn} class:text-gray-900={isOwn} class:text-gray-700={!isOwn}>
												{r.userFullname ?? 'Anoniem'}
											</td>
										{/if}
										<td class="py-2 pr-4">
											{#if r.status === 'reservation'}
												<span class="text-blue-600">Reservering</span>
											{:else if r.status === 'waitlist'}
												<span class="text-amber-600">Wachtlijst</span>
											{:else}
												<span class="text-green-600">Aangemeld</span>
											{/if}
										</td>
										{#each detail.fields as field (field.id)}
											<td class="py-2 pr-4 text-gray-600">
												{r.fieldValues.find((v) => v.fieldId === field.id)?.value ?? '—'}
											</td>
										{/each}
										{#if hasRowActions}
											<td class="py-2 pl-2 whitespace-nowrap">
												{#if isOwn}
													<div class="flex items-center gap-1">
														{#if detail.allowUpdate}
															<button
																onclick={openEdit}
																title="Bewerken"
																class="rounded p-1 text-gray-400 hover:bg-blue-100 hover:text-blue-600"
															>
																<svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
																	<path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
																</svg>
															</button>
														{/if}
														{#if detail.allowDelete}
															<button
																onclick={() => deleteDialog?.showModal()}
																title="Afmelden"
																class="rounded p-1 text-gray-400 hover:bg-red-100 hover:text-red-600"
															>
																<svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
																	<path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
																</svg>
															</button>
														{/if}
													</div>
												{/if}
											</td>
										{/if}
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</div>
		{:else if detail.hideSignups && detail.myResponse}
			<!-- Compact own-response indicator when the full list is hidden -->
			<HasPermission permission={BluePermission.SignupRespond}>
				<div class="flex items-center gap-3 rounded-lg bg-blue-50 px-4 py-3 text-sm">
					<span class="font-medium text-gray-700">Jouw aanmelding:</span>
					{#if detail.myResponse.status === 'reservation'}
						<span class="text-blue-600">Reservering</span>
					{:else if detail.myResponse.status === 'waitlist'}
						<span class="text-amber-600">Wachtlijst</span>
					{:else}
						<span class="text-green-600">Aangemeld</span>
					{/if}
					{#if isOpen}
						{#if detail.allowUpdate}
							<button
								onclick={openEdit}
								title="Bewerken"
								class="rounded p-1 text-gray-400 hover:bg-blue-100 hover:text-blue-600"
							>
								<svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
									<path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
								</svg>
							</button>
						{/if}
						{#if detail.allowDelete}
							<button
								onclick={() => deleteDialog?.showModal()}
								title="Afmelden"
								class="rounded p-1 text-gray-400 hover:bg-red-100 hover:text-red-600"
							>
								<svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
									<path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
								</svg>
							</button>
						{/if}
					{/if}
				</div>
			</HasPermission>
		{:else if !isOpen}
			<p class="text-sm text-gray-500">Deze inschrijving is gesloten.</p>
		{/if}
	</div>

	<!-- Aanmelden / Bewerken dialog -->
	<Modal bind:dialog={aanmeldenModal}>
		<div class="p-6">
			<h2 class="text-lg font-semibold text-gray-800 mb-4">
				{isEditing ? 'Aanmelding bewerken' : 'Aanmelden'}
			</h2>
			<form
				onsubmit={(e) => { e.preventDefault(); isEditing ? handleUpdate() : handleSubmit(); }}
				class="space-y-4"
			>
				{#if detail.fields.length === 0 && !isEditing}
					<p class="text-sm text-gray-600">Bevestig je aanmelding.</p>
				{/if}
				{#each detail.fields as field (field.id)}
					<FormField label={field.title} errors={responseForm.errorsFor(field.id)}>
						{#snippet children(invalid)}
							{#if field.type === SignupInputFieldType.Checkbox}
								<input
									type="checkbox"
									checked={fieldValues[field.id] === 'true'}
									onchange={(e) => {
										fieldValues[field.id] = (e.target as HTMLInputElement).checked
											? 'true'
											: 'false';
									}}
									class="h-4 w-4 rounded {invalid ? 'border-red-400' : 'border-gray-300'}"
								/>
							{:else if field.type === SignupInputFieldType.Textarea}
								<textarea
									bind:value={fieldValues[field.id]}
									rows="3"
									class="w-full rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
								></textarea>
							{:else if field.type === SignupInputFieldType.NumberField}
								<input
									type="number"
									bind:value={fieldValues[field.id]}
									class="w-full rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
								/>
							{:else if field.type === SignupInputFieldType.RadioList && field.options}
								{#each field.options.split(',').map(s => s.trim()).filter(Boolean) as opt (opt)}
									<label class="flex items-center gap-2">
										<input
											type="radio"
											name={field.id}
											value={opt}
											checked={fieldValues[field.id] === opt}
											onchange={() => { fieldValues[field.id] = opt; }}
										/>
										{opt}
									</label>
								{/each}
							{:else if field.type === SignupInputFieldType.OtherMember}
								<MemberPicker bind:value={memberValues[field.id]} {invalid} />
							{:else}
								<input
									type="text"
									bind:value={fieldValues[field.id]}
									class="w-full rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
								/>
							{/if}
						{/snippet}
					</FormField>
				{/each}
				{#if responseForm.formError}
					<p class="text-sm text-red-600">{responseForm.formError}</p>
				{/if}
				<div class="flex justify-end gap-2 pt-2">
					<Button variant="secondary" onclick={() => aanmeldenModal?.close()} type="button">
						Annuleren
					</Button>
					<Button type="submit" variant="primary" loading={responseForm.submitting}>
						{isEditing ? 'Opslaan' : 'Aanmelden'}
					</Button>
				</div>
			</form>
		</div>
	</Modal>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je je wilt afmelden?"
		confirmLabel="Afmelden"
		cancelLabel="Annuleren"
		onConfirm={handleDelete}
	/>
{/if}
