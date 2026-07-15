<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import {
		ConfirmDialog,
		Button,
		Spinner,
		MailingForm,
		MailingTargetPicker,
		MailingStats,
		MailingPreviewPanel,
		Pagination,
		breadcrumbs
	} from '$lib';
	import { MailTemplateKind, MailingStatus } from '$lib/api/apiClient';
	import type {
		MailLayoutDto,
		MailSenderInfoDto,
		MailTemplateDto,
		MailingDto,
		MailingStatsDto,
		MailingRecipientDto,
		MemberClusterDto,
		SeasonDto,
		UserGroupInstanceDto,
		UpsertMailingRequest
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	let mailing = $state<MailingDto | null>(null);
	let senders = $state<MailSenderInfoDto[]>([]);
	let templates = $state<MailTemplateDto[]>([]);
	let layouts = $state<MailLayoutDto[]>([]);
	let clusters = $state<MemberClusterDto[]>([]);
	let groupInstances = $state<UserGroupInstanceDto[]>([]);
	let stats = $state<MailingStatsDto | null>(null);
	let recipients = $state<MailingRecipientDto[]>([]);
	let recipientsPage = $state(1);
	let recipientsTotalCount = $state(0);
	const recipientsPageSize = 25;
	let resolvedTargetCount = $state<number | null>(null);

	let loading = $state(true);
	let error = $state(false);
	let sending = $state(false);
	let sendingProof = $state(false);
	let deleting = $state(false);
	let sendDialog = $state<HTMLDialogElement>();
	let deleteDialog = $state<HTMLDialogElement>();

	let selectedClusterIds = $state<string[]>([]);
	let selectedGroupInstanceIds = $state<string[]>([]);

	const isDraft = $derived(mailing?.status === MailingStatus.Draft);
	const recipientsTotalPages = $derived(Math.ceil(recipientsTotalCount / recipientsPageSize) || 1);

	async function loadRecipients() {
		const result = await apiClient.recipients(params.id, recipientsPage, recipientsPageSize);
		recipients = result.items;
		recipientsTotalCount = result.totalCount;
	}

	async function loadResolvedTargetCount() {
		resolvedTargetCount = await apiClient.targetCount(params.id);
	}

	async function loadAll() {
		[mailing, senders, templates, layouts, clusters, groupInstances, stats] = await Promise.all([
			apiClient.mailingsGET(params.id),
			apiClient.senders(),
			apiClient.mailTemplatesAll(MailTemplateKind.Mailing),
			apiClient.mailLayoutsAll(),
			apiClient.memberClustersAll(),
			loadCurrentSeasonGroupInstances(),
			apiClient.stats(params.id)
		]);
		selectedClusterIds = mailing.targetClusters.map((t) => t.memberClusterId);
		selectedGroupInstanceIds = mailing.targetGroupInstances.map((t) => t.userGroupInstanceId);
		await Promise.all([loadRecipients(), loadResolvedTargetCount()]);
	}

	async function loadCurrentSeasonGroupInstances(): Promise<UserGroupInstanceDto[]> {
		const [seasons, instances]: [SeasonDto[], UserGroupInstanceDto[]] = await Promise.all([
			apiClient.seasonsAll(),
			apiClient.userGroupInstancesAll()
		]);
		const currentSeasonId = seasons.find((s) => s.isCurrent)?.id;
		return currentSeasonId ? instances.filter((i) => i.seasonId === currentSeasonId) : instances;
	}

	onMount(async () => {
		try {
			await loadAll();
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	});

	$effect(() => {
		if (mailing) {
			breadcrumbs.set([
				{ label: 'Mail', href: '/tools/mail' },
				{ label: 'Mailings', href: '/tools/mail/mailings' },
				{ label: mailing.subject }
			]);
		}
		return () => breadcrumbs.clear();
	});

	$effect(() => {
		void recipientsPage;
		if (!loading) loadRecipients();
	});

	async function handleEdit(request: UpsertMailingRequest) {
		mailing = await apiClient.mailingsPUT(params.id, request);
	}

	let targetsSyncing = false;

	async function syncTargets() {
		if (targetsSyncing) return;
		targetsSyncing = true;
		try {
			const currentClusterIds = mailing!.targetClusters.map((t) => t.memberClusterId);
			const currentInstanceIds = mailing!.targetGroupInstances.map((t) => t.userGroupInstanceId);

			for (const id of selectedClusterIds.filter((id) => !currentClusterIds.includes(id))) {
				mailing = await apiClient.clustersPOST(params.id, id);
			}
			for (const id of currentClusterIds.filter((id) => !selectedClusterIds.includes(id))) {
				await apiClient.clustersDELETE(params.id, id);
				mailing = await apiClient.mailingsGET(params.id);
			}
			for (const id of selectedGroupInstanceIds.filter((id) => !currentInstanceIds.includes(id))) {
				mailing = await apiClient.groupinstancesPOST(params.id, id);
			}
			for (const id of currentInstanceIds.filter((id) => !selectedGroupInstanceIds.includes(id))) {
				await apiClient.groupinstancesDELETE(params.id, id);
				mailing = await apiClient.mailingsGET(params.id);
			}
			await loadResolvedTargetCount();
		} finally {
			targetsSyncing = false;
		}
	}

	$effect(() => {
		void selectedClusterIds;
		void selectedGroupInstanceIds;
		if (!loading && mailing) syncTargets();
	});

	async function handleSendProof() {
		sendingProof = true;
		try {
			await apiClient.proof(params.id);
			mailing = await apiClient.mailingsGET(params.id);
		} finally {
			sendingProof = false;
		}
	}

	async function handleSend() {
		sending = true;
		try {
			await apiClient.send(params.id);
			mailing = await apiClient.mailingsGET(params.id);
			stats = await apiClient.stats(params.id);
			await loadRecipients();
		} finally {
			sending = false;
		}
	}

	async function handleDelete() {
		deleting = true;
		try {
			await apiClient.mailingsDELETE(params.id);
			goto(resolve('/tools/mail/mailings'));
		} finally {
			deleting = false;
		}
	}
</script>

{#if loading}
	<Spinner />
{:else if error || !mailing}
	<p class="text-sm text-gray-600">Mailing kon niet worden geladen.</p>
{:else}
	<div class="flex items-start justify-between">
		<h1 class="text-2xl font-bold text-gray-900">{mailing.subject}</h1>
		<div class="flex gap-2">
			{#if isDraft}
				<Button
					variant="secondary"
					size="sm"
					onclick={() => deleteDialog?.showModal()}
					loading={deleting}
				>
					Verwijderen
				</Button>
			{/if}
			<Button
				variant="secondary"
				size="sm"
				onclick={handleSendProof}
				loading={sendingProof}
				disabled={!mailing.senderKey}
			>
				Proefverzending ({mailing.proofSendCount})
			</Button>
			<Button variant="primary" size="sm" onclick={() => sendDialog?.showModal()} loading={sending}>
				Verzenden
			</Button>
		</div>
	</div>

	<ConfirmDialog
		bind:dialog={sendDialog}
		message="Weet je zeker dat je '{mailing.subject}' wil verzenden naar {resolvedTargetCount ??
			'...'} {resolvedTargetCount === 1
			? 'ontvanger'
			: 'ontvangers'}? Dit kan niet ongedaan worden gemaakt."
		confirmLabel="Verzenden"
		onConfirm={handleSend}
	/>

	<ConfirmDialog
		bind:dialog={deleteDialog}
		message="Weet je zeker dat je '{mailing.subject}' wil verwijderen?"
		confirmLabel="Verwijderen"
		onConfirm={handleDelete}
	/>

	<div class="mt-6">
		<h2 class="text-sm font-semibold text-gray-700">Doelgroepen</h2>
		<div class="mt-2">
			<MailingTargetPicker
				{clusters}
				{groupInstances}
				bind:selectedClusterIds
				bind:selectedGroupInstanceIds
			/>
			<p class="mt-2 text-sm text-gray-600">
				{#if resolvedTargetCount === null}
					Aantal ontvangers wordt berekend…
				{:else}
					Dit bereikt momenteel <span class="font-medium text-gray-900">{resolvedTargetCount}</span>
					{resolvedTargetCount === 1 ? 'ontvanger' : 'ontvangers'}.
				{/if}
			</p>
		</div>
	</div>

	{#if isDraft}
		<div class="mt-6">
			<MailingForm
				{mailing}
				{senders}
				{templates}
				{layouts}
				submitLabel="Opslaan"
				onSubmit={handleEdit}
			/>
		</div>
	{:else}
		<div class="mt-6">
			<MailingPreviewPanel {mailing} />
		</div>
	{/if}

	<div class="mt-8">
		<h2 class="text-sm font-semibold text-gray-700">Statistieken</h2>
		<div class="mt-2">
			{#if stats}
				<MailingStats {stats} />
			{/if}
		</div>
	</div>

	<div class="mt-8">
		<h2 class="text-sm font-semibold text-gray-700">Ontvangers</h2>
		<table class="mt-2 w-full text-sm">
			<thead>
				<tr class="border-b border-gray-200 text-left text-gray-500">
					<th class="py-1.5 font-medium">Naam</th>
					<th class="py-1.5 font-medium">E-mail</th>
					<th class="py-1.5 font-medium">Verzonden</th>
					<th class="py-1.5 font-medium">Geopend</th>
				</tr>
			</thead>
			<tbody>
				{#each recipients as recipient (recipient.id)}
					<tr class="border-b border-gray-100">
						<td class="py-1.5 text-gray-900">{recipient.fullName}</td>
						<td class="py-1.5 text-gray-500">{recipient.email}</td>
						<td class="py-1.5 text-gray-500">{recipient.sent ? 'Ja' : 'Nee'}</td>
						<td class="py-1.5 text-gray-500"
							>{recipient.opened ? `Ja (${recipient.openCount}x)` : 'Nee'}</td
						>
					</tr>
				{:else}
					<tr>
						<td colspan="4" class="py-3 text-center text-gray-400">Geen ontvangers.</td>
					</tr>
				{/each}
			</tbody>
		</table>
		<Pagination
			page={recipientsPage}
			totalPages={recipientsTotalPages}
			onPageChange={(p) => (recipientsPage = p)}
		/>
	</div>
{/if}
