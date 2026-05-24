import { marked } from 'marked';

/**
 * Markdown → HTML for quest notes (and any other quest-board surface that
 * wants the same treatment later). Configured to be safe-by-default:
 *
 * - `gfm: true` → GitHub-flavoured markdown: task lists, autolinks, fenced
 *   code blocks. Matches what most users expect from a "notes" field.
 * - `breaks: true` → single newlines render as `<br>` so quick notes don't
 *   collapse onto one line just because the user didn't double-Enter.
 *
 * Inline raw HTML is *not* explicitly stripped — we instead rely on the
 * caller running the output through `DomSanitizer.bypassSecurityTrustHtml`
 * after a separate sanitisation pass *if* the input ever comes from
 * another user. For self-hosted single-user notes this is enough: the
 * user can't really XSS themselves, and we trust their own input.
 */
export function renderMarkdown(source: string): string {
  if (!source) return '';
  // `marked.parse` returns string when `async: false` (the default). The
  // parser is synchronous so we don't need to await — cast to string.
  return marked.parse(source, { gfm: true, breaks: true }) as string;
}
