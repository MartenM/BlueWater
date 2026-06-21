export interface FeaturedItem {
	title: string;
	summary: string;
	href: string;
	imageUrl?: string;
}

// Hardcoded for now; shaped to match what the planned featured-content API endpoint will
// return, so swapping this for a fetched value later shouldn't require changing FeaturedPane.
export const featuredItem: FeaturedItem = {
	title: 'Open dag: kom roeien bij Gyas',
	summary:
		'Nieuw in Groningen of altijd al willen roeien? Op onze open dag laten we je kennismaken met de boot, de loods en de club.',
	href: '/news/open-dag',
	imageUrl: '/images/media/kleine-club-hok.jpg'
};
