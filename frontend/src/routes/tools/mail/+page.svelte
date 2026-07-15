<script lang="ts">
	import { resolve } from '$app/paths';
	import { breadcrumbs } from '$lib';

	$effect(() => {
		breadcrumbs.set([{ label: 'Mail' }]);
		return () => breadcrumbs.clear();
	});
</script>

<h1 class="text-2xl font-bold text-gray-900">Mail</h1>

<div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
	<a
		href={resolve('/tools/mail/templates')}
		class="rounded-md border border-gray-200 p-4 hover:bg-gray-50"
	>
		<h2 class="font-semibold text-gray-900">Sjablonen</h2>
		<p class="mt-1 text-sm text-gray-500">Beheer mail- en transactionele sjablonen.</p>
	</a>
	<a
		href={resolve('/tools/mail/layouts')}
		class="rounded-md border border-gray-200 p-4 hover:bg-gray-50"
	>
		<h2 class="font-semibold text-gray-900">Layouts</h2>
		<p class="mt-1 text-sm text-gray-500">Beheer header/footer layouts voor sjablonen.</p>
	</a>
	<a
		href={resolve('/tools/mail/mailings')}
		class="rounded-md border border-gray-200 p-4 hover:bg-gray-50"
	>
		<h2 class="font-semibold text-gray-900">Mailings</h2>
		<p class="mt-1 text-sm text-gray-500">
			Stel mailings samen, verzend ze en bekijk statistieken.
		</p>
	</a>
</div>
