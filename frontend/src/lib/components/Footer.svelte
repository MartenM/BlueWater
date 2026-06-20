<script lang="ts">
	import Mail from '@lucide/svelte/icons/mail';
	import MapPin from '@lucide/svelte/icons/map-pin';
	import Phone from '@lucide/svelte/icons/phone';
	import { contactDetails } from '$lib/footer';
</script>

<footer class="border-t border-gray-200 bg-gray-50">
	<div class="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
		<h2 class="text-sm font-semibold text-black">Contact</h2>
		<ul class="mt-3 space-y-2 text-sm text-gray-600">
			{#if contactDetails.address}
				<li class="flex items-start gap-2">
					<MapPin class="mt-0.5 size-4 shrink-0" />
					<span>{contactDetails.address}</span>
				</li>
			{/if}
			{#if contactDetails.email}
				<li class="flex items-center gap-2">
					<Mail class="size-4 shrink-0" />
					<a href="mailto:{contactDetails.email}" class="hover:text-primary-hover">
						{contactDetails.email}
					</a>
				</li>
			{/if}
			{#if contactDetails.phone}
				<li class="flex items-center gap-2">
					<Phone class="size-4 shrink-0" />
					<a href="tel:{contactDetails.phone}" class="hover:text-primary-hover">
						{contactDetails.phone}
					</a>
				</li>
			{/if}
		</ul>
		<p class="mt-6 border-t border-gray-200 pt-4 text-xs text-gray-500">
			Runs on and is made using <span class="font-medium text-gray-700">Bluewater</span>
		</p>
	</div>
</footer>
