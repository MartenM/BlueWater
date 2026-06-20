<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.png';
	import { NavBar } from '$lib';

	let { children } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>
<NavBar />
{@render children()}
