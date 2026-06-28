<script lang="ts">
	let { label = 'Laden...' }: { label?: string } = $props();
</script>

<div class="flex items-center justify-center gap-2 py-8 text-sm text-gray-500">
	<svg class="h-4 w-4 animate-spin text-primary" viewBox="0 0 24 24" fill="none">
		<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
		<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
	</svg>
	<span>{label}</span>
</div>
