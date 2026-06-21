export interface NewsItem {
	id: string;
	title: string;
	summary: string;
	publishedAt: string;
	href: string;
}

// Hardcoded for now; shaped to match what the planned news API endpoint will return,
// so swapping this for a fetched value later shouldn't require changing NewsList.
export const newsItems: NewsItem[] = [
	{
		id: '1',
		title: 'Inschrijving Nieuwjaarsregatta geopend',
		summary:
			'Vanaf vandaag kun je je inschrijven voor de Nieuwjaarsregatta. Net als voorgaande jaren strijden ploegen van alle niveaus, van beginnende stuurlui tot onze ervaren wedstrijdploegen, om de eerste plek op de erelijst. Plekken zijn beperkt, dus wacht niet te lang met inschrijven. Na de wedstrijd is er traditiegetrouw een borrel in de loods om het nieuwe jaar samen te beginnen.',
		publishedAt: '2026-06-18',
		href: '/news/1'
	},
	{
		id: '2',
		title: 'Nieuwe boten in de loods',
		summary:
			'Het bestuur heeft twee nieuwe skiffs aangeschaft ter vervanging van de oudste boten in de vloot. Beide boten zijn deze week geleverd en na een korte keuringsperiode vanaf vrijdag beschikbaar via het botenboek. Materiaalcommissarissen geven tijdens de reguliere trainingsavonden een korte introductie over het op- en afriggeren, zodat iedereen er veilig mee het water op kan.',
		publishedAt: '2026-06-12',
		href: '/news/2'
	},
	{
		id: '3',
		title: 'Terugblik voorjaarscompetitie',
		summary:
			'Een geslaagd weekend met meerdere podiumplekken voor onze ploegen tijdens de voorjaarscompetitie. Vooral de damesacht en de mannen-vier wisten te overtuigen, met een tweede en derde plaats in hun categorie. Bekijk de volledige uitslagen, het wedstrijdverslag en de foto’s van het weekend op onze site, en bedank de stuurlui en coaches die het team dit seizoen begeleid hebben.',
		publishedAt: '2026-06-05',
		href: '/news/3'
	}
];
