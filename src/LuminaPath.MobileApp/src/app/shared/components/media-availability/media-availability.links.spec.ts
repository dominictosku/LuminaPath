import { buildAvailabilityLinks } from './media-availability.links';

describe('buildAvailabilityLinks', () => {
  it('builds movie streaming lookups with the release year', () => {
    const links = buildAvailabilityLinks({
      kind: 'movies',
      title: 'Dune',
      releaseDate: '2021-09-03T00:00:00Z',
    });

    expect(links[0].id).toBe('justwatch');
    expect(links[0].href).toBe('https://www.justwatch.com/us/search?q=Dune%202021');
    expect(links.some((link) => link.id === 'reelgood')).toBeTrue();
    expect(links.some((link) => link.id === 'tmdb')).toBeTrue();
  });

  it('builds anime-specific provider lookups', () => {
    const links = buildAvailabilityLinks({
      kind: 'animes',
      title: 'Frieren: Beyond Journey End',
    });

    expect(links.map((link) => link.id)).toEqual(['justwatch', 'crunchyroll', 'livechart']);
    expect(links[1].href).toBe('https://www.crunchyroll.com/search?q=Frieren%3A%20Beyond%20Journey%20End');
  });

  it('builds game trailer lookups without adding a release year', () => {
    const links = buildAvailabilityLinks({
      kind: 'games',
      title: 'Hades',
      releaseDate: new Date('2020-09-17T00:00:00Z'),
    });

    expect(links[0].id).toBe('youtube-trailers');
    expect(links[0].href).toBe('https://www.youtube.com/results?search_query=Hades%20official%20trailer%20game');
    expect(links.some((link) => link.id === 'steam-videos')).toBeTrue();
  });

  it('returns no links for empty titles', () => {
    expect(buildAvailabilityLinks({ kind: 'series', title: '   ' })).toEqual([]);
  });
});
