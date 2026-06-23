<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.png';
	import { Footer, NavBar, session } from '$lib';
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();

	$effect(() => {
		session.setUser(data.user);
	});
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>
<div class="flex min-h-screen flex-col">
	<NavBar />
	<main class="flex-1">
		{@render children()}
	</main>
	<Footer />
</div>
