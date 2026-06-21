<script lang="ts">
	import type { Snippet } from 'svelte';
	import Info from '@lucide/svelte/icons/info';
	import CircleCheck from '@lucide/svelte/icons/circle-check';
	import TriangleAlert from '@lucide/svelte/icons/triangle-alert';
	import CircleX from '@lucide/svelte/icons/circle-x';
	import { AlertLevel } from '$lib/alert';

	let { level = AlertLevel.Info, children }: { level?: AlertLevel; children: Snippet } = $props();

	const styles: Record<AlertLevel, string> = {
		[AlertLevel.Info]: 'border-blue-200 bg-blue-50 text-blue-700',
		[AlertLevel.Success]: 'border-green-200 bg-green-50 text-green-700',
		[AlertLevel.Warning]: 'border-amber-200 bg-amber-50 text-amber-700',
		[AlertLevel.Danger]: 'border-red-200 bg-red-50 text-red-700'
	};

	const icons = {
		[AlertLevel.Info]: Info,
		[AlertLevel.Success]: CircleCheck,
		[AlertLevel.Warning]: TriangleAlert,
		[AlertLevel.Danger]: CircleX
	};

	const Icon = $derived(icons[level]);
</script>

<div
	class="flex items-start gap-2 rounded-md border px-3 py-2 text-sm {styles[level]}"
	role="alert"
>
	<Icon class="mt-0.5 size-4 shrink-0" />
	<div>{@render children()}</div>
</div>
