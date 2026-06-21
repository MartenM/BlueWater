<script lang="ts">
	import { FeaturedPane, Jumbotron, NewsList } from '$lib';
</script>

<Jumbotron />

<div class="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
	<div class="grid gap-12 lg:grid-cols-3">
		<div class="lg:col-span-2">
			<NewsList limit={4} />
		</div>
		<div>
			<FeaturedPane />
		</div>
	</div>
</div>
