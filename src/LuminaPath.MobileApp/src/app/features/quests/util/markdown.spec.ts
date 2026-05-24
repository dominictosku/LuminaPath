import { renderMarkdown } from './markdown';

describe('renderMarkdown', () => {
  it('renders markdown formatting', () => {
    const html = renderMarkdown('**bold**\nnext');

    expect(html).toContain('<strong>bold</strong>');
    expect(html).toContain('<br>');
  });

  it('strips executable HTML from stored notes', () => {
    const html = renderMarkdown('<img src=x onerror="alert(1)"><script>alert(1)</script>[x](javascript:alert(1))');

    expect(html).not.toContain('<script');
    expect(html).not.toContain('onerror');
    expect(html).not.toContain('javascript:');
  });
});
