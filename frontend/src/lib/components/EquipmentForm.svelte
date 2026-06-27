<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { UpsertEquipmentRequest } from '$lib/api/apiClient';
	import { AlertLevel } from '$lib/alert';
	import { FormState } from '$lib/forms/formState.svelte';
	import type {
		EquipmentDto,
		EquipmentTypeDto,
		ManufacturerDto,
		OarSetDto
	} from '$lib/api/apiClient';
	import { Button, apiClient } from '$lib';
	import BlueAlert from './BlueAlert.svelte';
	import FormField from './FormField.svelte';

	let {
		equipment,
		submitLabel,
		onSubmit
	}: {
		equipment?: EquipmentDto;
		submitLabel: string;
		onSubmit: (request: UpsertEquipmentRequest) => Promise<void>;
	} = $props();

	let name = $state(untrack(() => equipment?.name) ?? '');
	let description = $state(untrack(() => equipment?.description) ?? '');
	let equipmentTypeId = $state(untrack(() => equipment?.equipmentTypeId) ?? '');
	let manufacturerId = $state(untrack(() => equipment?.manufacturerId) ?? '');
	let oarSetId = $state(untrack(() => equipment?.oarSetId) ?? '');
	let freeFleet = $state(untrack(() => equipment?.freeFleet) ?? false);
	let outOfOrder = $state(untrack(() => equipment?.outOfOrder) ?? false);
	let rowersWeight = $state(untrack(() => equipment?.rowersWeight?.toString()) ?? '');
	let rowersWeightMax = $state(untrack(() => equipment?.rowersWeightMax?.toString()) ?? '');
	let dateBuild = $state(untrack(() => toDateInput(equipment?.dateBuild)));
	let dateBought = $state(untrack(() => toDateInput(equipment?.dateBought)));
	let dateSold = $state(untrack(() => toDateInput(equipment?.dateSold)));

	let types = $state<EquipmentTypeDto[]>([]);
	let manufacturers = $state<ManufacturerDto[]>([]);
	let oarSets = $state<OarSetDto[]>([]);

	const form = new FormState();

	function toDateInput(d: Date | undefined): string {
		if (!d) return '';
		return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
	}

	function parseDate(s: string): Date | undefined {
		return s ? new Date(s) : undefined;
	}

	onMount(async () => {
		try {
			[types, manufacturers, oarSets] = await Promise.all([
				apiClient.typesAll2(),
				apiClient.manufacturersAll(),
				apiClient.oarSetsAll()
			]);
		} catch {
			// non-fatal
		}
	});

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		form.submit(() =>
			onSubmit(
				new UpsertEquipmentRequest({
					name,
					description: description || undefined,
					equipmentTypeId: equipmentTypeId || undefined,
					manufacturerId: manufacturerId || undefined,
					oarSetId: oarSetId || undefined,
					freeFleet,
					outOfOrder,
					rowersWeight: rowersWeight ? parseInt(rowersWeight) : undefined,
					rowersWeightMax: rowersWeightMax ? parseInt(rowersWeightMax) : undefined,
					dateBuild: parseDate(dateBuild),
					dateBought: parseDate(dateBought),
					dateSold: parseDate(dateSold)
				})
			)
		);
	}
</script>

<form class="flex flex-col gap-4" onsubmit={handleSubmit}>
	<FormField label="Naam" errors={form.errorsFor('name')}>
		{#snippet children(invalid)}
			<input
				type="text"
				required
				bind:value={name}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			/>
		{/snippet}
	</FormField>

	<FormField label="Omschrijving" errors={form.errorsFor('description')}>
		{#snippet children(invalid)}
			<textarea
				rows="3"
				bind:value={description}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"></textarea>
		{/snippet}
	</FormField>

	<FormField label="Type" errors={form.errorsFor('equipmentTypeId')}>
		{#snippet children(invalid)}
			<select
				bind:value={equipmentTypeId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— geen —</option>
				{#each types as t (t.id)}
					<option value={t.id}>{t.name} ({t.code})</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	<FormField label="Fabrikant" errors={form.errorsFor('manufacturerId')}>
		{#snippet children(invalid)}
			<select
				bind:value={manufacturerId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— geen —</option>
				{#each manufacturers as m (m.id)}
					<option value={m.id}>{m.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	<FormField label="Riemstel" errors={form.errorsFor('oarSetId')}>
		{#snippet children(invalid)}
			<select
				bind:value={oarSetId}
				class="rounded-md focus:border-primary focus:ring-primary {invalid
					? 'border-red-400'
					: 'border-gray-300'}"
			>
				<option value="">— geen —</option>
				{#each oarSets as o (o.id)}
					<option value={o.id}>{o.name}</option>
				{/each}
			</select>
		{/snippet}
	</FormField>

	<div class="grid grid-cols-2 gap-4">
		<FormField label="Gewicht roeiers (kg)" errors={form.errorsFor('rowersWeight')}>
			{#snippet children(invalid)}
				<input
					type="number"
					min="1"
					bind:value={rowersWeight}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Max. gewicht roeiers (kg)" errors={form.errorsFor('rowersWeightMax')}>
			{#snippet children(invalid)}
				<input
					type="number"
					min="1"
					bind:value={rowersWeightMax}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Bouwjaar" errors={form.errorsFor('dateBuild')}>
			{#snippet children(invalid)}
				<input
					type="date"
					bind:value={dateBuild}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Aanschafdatum" errors={form.errorsFor('dateBought')}>
			{#snippet children(invalid)}
				<input
					type="date"
					bind:value={dateBought}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>

		<FormField label="Verkoopdatum" errors={form.errorsFor('dateSold')}>
			{#snippet children(invalid)}
				<input
					type="date"
					bind:value={dateSold}
					class="rounded-md focus:border-primary focus:ring-primary {invalid
						? 'border-red-400'
						: 'border-gray-300'}"
				/>
			{/snippet}
		</FormField>
	</div>

	<div class="flex flex-col gap-2">
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input
				type="checkbox"
				bind:checked={freeFleet}
				class="rounded text-primary focus:ring-primary"
			/>
			Vrije vloot (te boeken door iedereen)
		</label>
		<label class="flex items-center gap-2 text-sm text-gray-700">
			<input
				type="checkbox"
				bind:checked={outOfOrder}
				class="rounded text-primary focus:ring-primary"
			/>
			Buiten gebruik
		</label>
	</div>

	{#if form.formError}
		<BlueAlert level={AlertLevel.Danger}>{form.formError}</BlueAlert>
	{/if}

	<div class="mt-2 self-start">
		<Button type="submit" loading={form.submitting}>{submitLabel}</Button>
	</div>
</form>
