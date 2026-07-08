<script lang="ts">
	import { onMount } from 'svelte';
	import { AvailabilityTimeline, OutingPlannerTeamList, Spinner, breadcrumbs } from '$lib';
	import {
		AvailabilityBlockInputDto,
		SetDayAvailabilityRequest,
		type AvailabilityMemberWeekDto,
		type InstanceWeekAvailabilityDto,
		type UserGroupInstanceDto
	} from '$lib/api/apiClient';
	import { apiClient } from '$lib/api/client';
	import { session } from '$lib/auth/session.svelte';
	import { extractApiError } from '$lib/forms/apiError';
	import {
		addDays,
		dateKey,
		fromApiTime,
		startOfWeek,
		toApiTime
	} from '$lib/constants/availability';
	import type { TimeBlock } from '$lib/utils/availabilityMerge';
	import type { PageProps } from './$types';

	let { params }: PageProps = $props();

	const dayLabels = [
		'Maandag',
		'Dinsdag',
		'Woensdag',
		'Donderdag',
		'Vrijdag',
		'Zaterdag',
		'Zondag'
	];

	let weekStart = $state(startOfWeek(new Date()));
	let data = $state<InstanceWeekAvailabilityDto | null>(null);
	let instance = $state<UserGroupInstanceDto | null>(null);
	let loading = $state(true);
	let error = $state(false);
	let dayErrors = $state<Record<string, string>>({});

	const days = $derived(Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)));
	const myUserId = $derived(session.user?.id ?? null);

	async function load() {
		loading = true;
		try {
			data = await apiClient.availability(params.instanceId, dateKey(weekStart));
			error = false;
		} catch {
			error = true;
		} finally {
			loading = false;
		}
	}

	async function loadInstance() {
		try {
			instance = await apiClient.userGroupInstancesGET(params.instanceId);
		} catch {
			instance = null;
		}
	}

	function blocksFor(member: AvailabilityMemberWeekDto, day: Date): TimeBlock[] {
		const key = dateKey(day);
		const dayData = member.days.find((d) => dateKey(d.date) === key);
		return (dayData?.blocks ?? []).map((b) => ({
			startTime: fromApiTime(b.startTime),
			endTime: fromApiTime(b.endTime)
		}));
	}

	async function saveDay(day: Date, blocks: TimeBlock[]) {
		const key = dateKey(day);
		try {
			const result = await apiClient.day(
				new SetDayAvailabilityRequest({
					date: day,
					blocks: blocks.map(
						(b) =>
							new AvailabilityBlockInputDto({
								startTime: toApiTime(b.startTime),
								endTime: toApiTime(b.endTime)
							})
					)
				})
			);
			if (data && myUserId) {
				for (const group of data.roleGroups) {
					const member = group.members.find((m) => m.userId === myUserId);
					const dayEntry = member?.days.find((d) => dateKey(d.date) === key);
					if (dayEntry) dayEntry.blocks = result;
				}
			}
			dayErrors = { ...dayErrors, [key]: '' };
		} catch (e) {
			dayErrors = { ...dayErrors, [key]: extractApiError(e).formError ?? 'Opslaan is mislukt.' };
		}
	}

	function prevWeek() {
		weekStart = addDays(weekStart, -7);
	}

	function nextWeek() {
		weekStart = addDays(weekStart, 7);
	}

	$effect(() => {
		void weekStart;
		load();
	});

	onMount(() => {
		loadInstance();
	});

	$effect(() => {
		breadcrumbs.set([
			{ label: 'Beschikbaarheid', href: '/tools/availability-planner' },
			{ label: instance ? `${instance.userGroupName} (${instance.seasonName})` : 'Team' }
		]);
		return () => breadcrumbs.clear();
	});
</script>

<div class="flex items-center justify-between">
	<div>
		<h1 class="text-2xl font-bold text-gray-900">Beschikbaarheid</h1>
		{#if instance}
			<p class="text-sm text-gray-500">{instance.userGroupName} · {instance.seasonName}</p>
		{/if}
	</div>
	<div class="flex items-center gap-2">
		<button
			type="button"
			onclick={prevWeek}
			class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
		>
			← Vorige week
		</button>
		<button
			type="button"
			onclick={nextWeek}
			class="rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-50"
		>
			Volgende week →
		</button>
	</div>
</div>

<div class="mt-6 lg:flex lg:items-start">
	<div class="lg:w-3/4">
		{#if loading}
			<Spinner />
		{:else if error}
			<p class="text-sm text-gray-600">Beschikbaarheid kon niet worden geladen.</p>
		{:else if data}
			<div class="space-y-8">
				{#each days as day, dayIndex (dateKey(day))}
					{@const key = dateKey(day)}
					<div>
						<h2 class="text-lg font-semibold text-gray-900">
							{dayLabels[dayIndex]}
							<span class="font-normal text-gray-500">
								{day.toLocaleDateString('nl-NL', { day: 'numeric', month: 'long' })}
							</span>
						</h2>
						{#if dayErrors[key]}
							<p class="mt-1 text-xs text-red-600">{dayErrors[key]}</p>
						{/if}
						<div class="mt-3 space-y-4">
							{#each data.roleGroups as group, groupIndex (group.userGroupCategoryRoleId ?? group.roleLabel)}
								<div>
									<h3 class="text-xs font-semibold tracking-wide text-gray-500 uppercase">
										{group.roleLabel}
									</h3>
									<div class="mt-1 space-y-1.5">
										{#each group.members as member, memberIndex (member.userId)}
											<div class="flex items-center gap-3">
												<span class="w-40 shrink-0 truncate text-sm text-gray-700">
													{member.fullname}
												</span>
												<div class="flex-1">
													<AvailabilityTimeline
														blocks={blocksFor(member, day)}
														editable={member.userId === myUserId}
														onchange={(blocks) => saveDay(day, blocks)}
														showHourLabels={groupIndex === 0 && memberIndex === 0}
													/>
												</div>
											</div>
										{/each}
									</div>
								</div>
							{/each}
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div
		class="mt-10 border-t border-gray-200 pt-6 lg:mt-0 lg:w-1/4 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8"
	>
		<OutingPlannerTeamList basePath="/tools/availability-planner" />
	</div>
</div>
