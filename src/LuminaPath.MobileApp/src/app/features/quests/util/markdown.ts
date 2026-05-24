import DOMPurify from 'dompurify';
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
 * `marked` does not sanitize raw HTML. DOMPurify runs after markdown parsing
 * so stored notes can keep useful markdown formatting without letting script
 * tags, event handlers, or javascript: URLs through to `[innerHTML]`.
 */
export function renderMarkdown(source: string): string {
  if (!source) return '';
  // `marked.parse` returns string when `async: false` (the default). The
  // parser is synchronous so we don't need to await — cast to string.
  const html = marked.parse(source, { gfm: true, breaks: true }) as string;
  return DOMPurify.sanitize(html, { RETURN_TRUSTED_TYPE: false });
}
