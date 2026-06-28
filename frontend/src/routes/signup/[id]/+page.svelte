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
	import { FormState } from '$lib/forms/formState.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	const signupId = params.id;

	let detail = $state<SignupDetailDto | null>(null);
	let loadError = $state(false);
	let deleteDialog = $state<HTMLDialogElement>();
	let aanmeldenModal = $state<HTMLDialogElement>();

	onMount(async () => {
		try {
			detail = await apiClient.signupsGET2(signupId);
		} catch {
			loadError = true;
		}
	});

	const responseForm = new FormState();
	const deleteForm = new FormState();

	let fieldValues = $state<Record<string, string>>({});

	$effect(() => {
		if (detail) {
			breadcrumbs.set([
				{ label: 'Inschrijvingen', href: resolve('/signup') },
				{ label: detail.title }
			]);
		}
		return () => breadcrumbs.clear();
	});

	$effect(() => {
		if (detail?.myResponse) {
			const vals: Record<string, string> = {};
			for (const fv of detail.myResponse.fieldValues) {
				vals[fv.fieldId] = fv.value ?? '';
			}
			fieldValues = vals;
		}
	});

	const isOpen = $derived(!detail?.endDate || new Date(detail.endDate) >= new Date());

	function formatDate(d: Date | string | undefined) {
		if (!d) return null;
		return new Date(d).toLocaleDateString('nl-NL', { day: 'numeric', month: 'long', year: 'numeric' });
	}

	async function handleSubmit() {
		if (!detail) return;
		const fvList = detail.fields.map(
			(f) => new FieldValueRequest({ fieldId: f.id, value: fieldValues[f.id] ?? null })
		);
		await responseForm.submit(async () => {
			await apiClient.responsesPOST(
				signupId,
				new SubmitResponseRequest({ fieldValues: fvList })
			);
			detail = await apiClient.signupsGET2(signupId);
			aanmeldenModal?.close();
		});
	}

	async function handleUpdate() {
		if (!detail?.myResponse) return;
		const fvList = detail.fields.map(
			(f) => new FieldValueRequest({ fieldId: f.id, value: fieldValues[f.id] ?? null })
		);
		await responseForm.submit(async () => {
			await apiClient.responsesPUT(
				signupId,
				detail!.myResponse!.id,
				new UpdateResponseRequest({ fieldValues: fvList })
			);
			detail = await apiClient.signupsGET2(signupId);
		});
	}

	async function handleDelete() {
		if (!detail?.myResponse) return;
		await deleteForm.submit(async () => {
			await apiClient.responsesDELETE2(signupId, detail!.myResponse!.id);
			detail = await apiClient.signupsGET2(signupId);
		});
	}
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
						<Button variant="primary" onclick={() => aanmeldenModal?.showModal()}>
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
								</tr>
							</thead>
							<tbody class="divide-y divide-gray-100">
								{#each detail.responses as r (r.id)}
									<tr>
										{#if !detail.anonymous}
											<td class="py-2 pr-4 text-gray-900">{r.userFullname ?? 'Anoniem'}</td>
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
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{/if}
			</div>
		{/if}

		<!-- My response (existing) -->
		<HasPermission permission={BluePermission.SignupRespond}>
			{#if detail.myResponse}
				<div class="border border-gray-200 rounded-lg p-6">
					<h2 class="text-lg font-semibold text-gray-800">Mijn aanmelding</h2>
					<p class="mt-1 text-sm text-gray-500">
						Status:
						{#if detail.myResponse.status === 'reservation'}
							<span class="text-blue-600">Reservering</span>
						{:else if detail.myResponse.status === 'waitlist'}
							<span class="text-amber-600">Wachtlijst</span>
						{:else}
							<span class="text-green-600">Aangemeld</span>
						{/if}
					</p>

					{#if detail.allowUpdate && isOpen}
						<form
							onsubmit={(e) => { e.preventDefault(); handleUpdate(); }}
							class="mt-4 space-y-4"
						>
							{#each detail.fields as field (field.id)}
								<FormField label={field.title} errors={responseForm.errorsFor(field.id)}>
									{#snippet children(invalid)}
										<input
											type="text"
											bind:value={fieldValues[field.id]}
											class="w-full rounded-md {invalid ? 'border-red-400' : 'border-gray-300'}"
										/>
									{/snippet}
								</FormField>
							{/each}
							{#if responseForm.formError}
								<p class="text-sm text-red-600">{responseForm.formError}</p>
							{/if}
							<Button type="submit" variant="primary" size="sm" loading={responseForm.submitting}>
								Opslaan
							</Button>
						</form>
					{/if}

					{#if detail.allowDelete && isOpen}
						<div class="mt-4">
							<Button
								variant="danger"
								size="sm"
								onclick={() => deleteDialog?.showModal?.()}
								loading={deleteForm.submitting}
							>
								Afmelden
							</Button>
							{#if deleteForm.formError}
								<p class="mt-1 text-sm text-red-600">{deleteForm.formError}</p>
							{/if}
						</div>
					{/if}
				</div>
			{:else if !isOpen}
				<p class="text-sm text-gray-500">Deze inschrijving is gesloten.</p>
			{/if}
		</HasPermission>
	</div>

	<!-- Aanmelden dialog -->
	<Modal bind:dialog={aanmeldenModal}>
		<div class="p-6">
			<form
				onsubmit={(e) => { e.preventDefault(); handleSubmit(); }}
				class="space-y-4"
			>
				{#if detail.fields.length === 0}
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
								{#each JSON.parse(field.options) as opt (opt)}
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
						Aanmelden
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
