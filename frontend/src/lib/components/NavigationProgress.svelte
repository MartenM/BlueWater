<script lang="ts">
	import { navigating } from '$app/stores';
</script>

{#if $navigating}
	<div class="fixed top-0 left-0 z-50 h-1 w-full overflow-hidden">
		<div class="animate-progress h-full w-full bg-primary origin-left"></div>
	</div>
{/if}

<style>
	@keyframes progress {
		0% {
			transform: translateX(-100%);
		}
		60% {
			transform: translateX(-10%);
		}
		100% {
			transform: translateX(0%);
		}
	}

	.animate-progress {
		animation: progress 2s cubic-bezier(0.1, 0.4, 0.2, 1) forwards;
	}
</style>
