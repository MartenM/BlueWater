<script lang="ts">
	import { onMount } from 'svelte';
	import { resolve } from '$app/paths';
	import { goto } from '$app/navigation';
	import {
		ConfirmDialog,
		Button,
		Spinner,
		breadcrumbs,
		OutingRosterManager,
		OutingChangelogList
	} from '$lib';
	import { OutingParticipantRole } from '$lib/api/apiClient';
	import type { OutingChangelogEntryDto, OutingDetailDto } from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { session } from '$lib/auth/session.svelte';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let outing = $state<OutingDetailDto | null>(null);
	let changelog = $state<OutingChangelogEntryDto[]>([]);
	let error = $state(false);
	let loading = $state(true);
	let actionError = $state<string | null>(null);
	let busy = $state(false);
	let now = $state(new Date());

	let deleteDialog = $state<HTMLDialogElement>();
	let didNotHappenDialog = $state<HTMLDialogElement>();

	const myParticipant = $derived(
		outing?.participants.find((p) => p.userId === session.user?.id) ?? null
	);

	const outingHasPassed = $derived(!!outing && outing.outingDate.getTime() <= now.getTime());

	const checkInWindowOpen = $derived(
		!!outing &&
			now.getTime() >= outing.outingDate.getTime() - 30 * 60000 &&
			now.getTime() <= outing.outingDate.getTime() + 3 * 60 * 60000
	);

	async function load() {
		loading = true;
		try {
			[outing, changelog] = await Promise.all([
				apiClient.outingsGET(params.id),
				apiClient.changelog(params.id)
			]);
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	onMount(() => {
		load();
		const timer = setInterval(() => (now = new Date()), 30000);
		return () => clearInterval(timer);
	});

	$effect(() => {
		if (outing) {
			breadcrumbs.set([
				{ label: 'Outing Planner', href: '/tools/outing-planner' },
				{
					label: outing.userGroupInstanceName,
					href: `/tools/outing-planner/instance/${outing.userGroupInstanceId}`
				},
				{ label: outing.outingDate.toLocaleDateString('nl-NL') }
			]);
		}
		return () => breadcrumbs.clear();
	});

	async function handleCheckIn() {
		busy = true;
		actionError = null;
		try {
			await apiClient.checkIn(params.id);
			await load();
		} catch {
			actionError = 'Inchecken is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function handleConfirm() {
		busy = true;
		actionError = null;
		try {
			await apiClient.confirm(params.id);
			await load();
		} catch {
			actionError = 'Bevestigen is mislukt. Probeer het later opnieuw.';
		} finally {
			busy = false;
		}
	}

	async function handleDidNotHappen() {
		busy = true;
		actionError = null;
		try {
			await apiClient.didNotHappen(params.id);
			goto(
				resolve('/tools/outing-planner/instance/[instanceId]', {
					instanceId: outing!.userGroupInstanceId
				})
			);
		} catch {
			actionError = 'Bijwerken is mislukt. Probeer het later opnieuw.';
			busy = false;
		}
	}

	async function handleDelete() {
		await apiClient.outingsDELETE(params.id);
		goto(
			resolve('/tools/outing-planner/instance/[instanceId]', {
				instanceId: outing!.userGroupInstanceId
			})
		);
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !outing}
	<p class="text-sm text-gray-600">Outing kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">
				{outing.outingDate.toLocaleString('nl-NL', { dateStyle: 'full', timeStyle: 'short' })}
			</h1>
			<p class="text-sm text-gray-500">{outing.userGroupInstanceName}</p>
		</div>
		{#if !outing.confirmed}
			<div class="flex gap-3">
				<Button
					href={resolve('/tools/outing-planner/[id]/edit', { id: params.id })}
					variant="secondary"
					size="sm"
				>
					Bewerken
				</Button>
				<Button variant="danger" size="sm" onclick={() => deleteDialog?.showModal()}>
					Verwijderen
				</Button>
			</div>
		{/if}
	</div>

	{#if outing.confirmed}
		<span
			class="mt-2 inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800"
		>
			Bevestigd
		</span>
	{/if}

	<div class="mt-6 flex flex-wrap gap-3">
		{#if myParticipant && myParticipant.role !== OutingParticipantRole.None && !myParticipant.checkedIn}
			<Button
				variant="secondary"
				size="sm"
				disabled={!checkInWindowOpen || busy}
				onclick={handleCheckIn}
			>
				Inchecken
			</Button>
		{/if}
		{#if outingHasPassed && !outing.confirmed}
			<Button variant="success" size="sm" disabled={busy} onclick={handleConfirm}>
				Bevestigen (heeft plaatsgevonden)
			</Button>
			<Button
				variant="warning"
				size="sm"
				disabled={busy}
				onclick={() => didNotHappenDialog?.showModal()}
			>
				Niet doorgegaan
			</Button>
		{/if}
	</div>

	{#if actionError}
		<p class="mt-3 text-sm text-red-600">{actionError}</p>
	{/if}

	<div class="mt-6 grid grid-cols-1 gap-8 lg:grid-cols-3">
		<div class="lg:col-span-1">
			<dl class="grid grid-cols-1 gap-4">
				<div>
					<dt class="text-sm font-medium text-gray-500">Boottype</dt>
					<dd class="mt-1 text-sm text-gray-900">
						{outing.boatTypeName ?? outing.boatTypeDifferent ?? '—'}
					</dd>
				</div>
				<div>
					<dt class="text-sm font-medium text-gray-500">Boot</dt>
					<dd class="mt-1 text-sm text-gray-900">{outing.boatName ?? '—'}</dd>
				</div>
				{#if outing.description}
					<div>
						<dt class="text-sm font-medium text-gray-500">Omschrijving</dt>
						<dd class="mt-1 text-sm text-gray-900">{outing.description}</dd>
					</div>
				{/if}
			</dl>

			<div class="mt-8">
				<OutingChangelogList entries={changelog} />
			</div>
		</div>

		<div class="lg:col-span-2">
			<OutingRosterManager {outing} onChanged={load} />
		</div>
	</div>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je deze outing wil verwijderen?"
		onConfirm={handleDelete}
	/>
	<ConfirmDialog
		bind:dialog={didNotHappenDialog}
		message="Markeer deze outing als niet doorgegaan? De outing wordt verwijderd."
		confirmLabel="Niet doorgegaan"
		onConfirm={handleDidNotHappen}
	/>
{/if}
