<script lang="ts">
	import { resolve } from '$app/paths';
</script>

<div class="mx-auto flex max-w-7xl flex-col items-center px-4 py-16 text-center sm:px-6 lg:px-8">
	<img
		src="/images/media/kleine-club-hok.jpg"
		alt="Klein clubhok"
		class="mb-8 max-h-80 w-auto rounded-md shadow-sm"
	/>
	<h1 class="text-2xl font-bold text-gray-900">Pagina niet gevonden</h1>
	<p class="mt-2 text-gray-600">Je bent de verkeerde kant opgeroeid.</p>
	<a
		href={resolve('/')}
		class="mt-6 rounded-md bg-primary px-4 py-2 text-primary-content hover:bg-primary-hover"
	>
		Terug naar home
	</a>
</div>
