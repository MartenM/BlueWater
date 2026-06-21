import { marked } from 'marked';
import sanitizeHtml from 'sanitize-html';

export function renderMarkdown(source: string): string {
	const html = marked.parse(source, { async: false, breaks: true });
	return sanitizeHtml(html, {
		allowedTags: sanitizeHtml.defaults.allowedTags.concat('img'),
		allowedAttributes: {
			...sanitizeHtml.defaults.allowedAttributes,
			img: ['src', 'alt']
		}
	});
}
