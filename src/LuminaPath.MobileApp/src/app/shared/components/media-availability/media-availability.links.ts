export type AvailabilityMediaKind = 'games' | 'movies' | 'animes' | 'series';

export interface AvailabilityLookupInput {
  kind: AvailabilityMediaKind;
  title: string | null | undefined;
  releaseDate?: Date | string | null;
}

export interface AvailabilityLink {
  id: string;
  label: string;
  description: string;
  href: string;
  icon: string;
  primary?: boolean;
}

export function buildAvailabilityLinks(input: AvailabilityLookupInput): AvailabilityLink[] {
  const title = normalizeTitle(input.title);
  if (!title) {
    return [];
  }

  const titledWithYear = appendReleaseYear(title, input.releaseDate);

  switch (input.kind) {
    case 'games':
      return [
        {
          id: 'youtube-trailers',
          label: 'YouTube',
          description: 'Official trailers and gameplay reveals',
          href: youtubeSearch(`${title} official trailer game`),
          icon: 'play-outline',
          primary: true,
        },
        {
          id: 'steam-videos',
          label: 'Steam',
          description: 'Store videos and launch trailers',
          href: `https://store.steampowered.com/search/?term=${encode(title)}`,
          icon: 'game-controller-outline',
        },
        {
          id: 'ign-videos',
          label: 'IGN',
          description: 'Trailers, previews, and gameplay clips',
          href: `https://www.ign.com/search?q=${encode(`${title} trailer`)}`,
          icon: 'newspaper-outline',
        },
      ];
    case 'animes':
      return [
        {
          id: 'justwatch',
          label: 'JustWatch',
          description: 'Streaming availability',
          href: justWatchSearch(titledWithYear),
          icon: 'search-outline',
          primary: true,
        },
        {
          id: 'crunchyroll',
          label: 'Crunchyroll',
          description: 'Anime streaming catalog',
          href: `https://www.crunchyroll.com/search?q=${encode(title)}`,
          icon: 'sparkles-outline',
        },
        {
          id: 'livechart',
          label: 'LiveChart',
          description: 'Broadcast and streaming listings',
          href: `https://www.livechart.me/search?q=${encode(title)}`,
          icon: 'calendar-outline',
        },
      ];
    case 'series':
      return [
        {
          id: 'justwatch',
          label: 'JustWatch',
          description: 'Streaming availability',
          href: justWatchSearch(titledWithYear),
          icon: 'search-outline',
          primary: true,
        },
        {
          id: 'reelgood',
          label: 'Reelgood',
          description: 'Streaming catalog lookup',
          href: `https://reelgood.com/search?q=${encode(titledWithYear)}`,
          icon: 'tv-outline',
        },
        {
          id: 'tmdb',
          label: 'TMDb',
          description: 'Watch-provider and title page search',
          href: `https://www.themoviedb.org/search?query=${encode(titledWithYear)}`,
          icon: 'film-outline',
        },
      ];
    case 'movies':
      return [
        {
          id: 'justwatch',
          label: 'JustWatch',
          description: 'Streaming availability',
          href: justWatchSearch(titledWithYear),
          icon: 'search-outline',
          primary: true,
        },
        {
          id: 'reelgood',
          label: 'Reelgood',
          description: 'Streaming catalog lookup',
          href: `https://reelgood.com/search?q=${encode(titledWithYear)}`,
          icon: 'film-outline',
        },
        {
          id: 'tmdb',
          label: 'TMDb',
          description: 'Watch-provider and title page search',
          href: `https://www.themoviedb.org/search?query=${encode(titledWithYear)}`,
          icon: 'open-outline',
        },
      ];
  }
}

export function availabilityHeading(kind: AvailabilityMediaKind): string {
  return kind === 'games' ? 'Trailers' : 'Streaming availability';
}

export function availabilityEyebrow(kind: AvailabilityMediaKind): string {
  return kind === 'games' ? 'Trailers' : 'Where to watch';
}

export function availabilityLead(kind: AvailabilityMediaKind): string {
  return kind === 'games'
    ? 'Official videos, store trailers, and preview clips.'
    : 'Provider lookups for streaming, rental, and purchase options.';
}

export function availabilityIcon(kind: AvailabilityMediaKind): string {
  return kind === 'games'
    ? 'play-outline'
    : kind === 'series'
      ? 'tv-outline'
      : kind === 'animes'
        ? 'sparkles-outline'
        : 'film-outline';
}

function justWatchSearch(query: string): string {
  return `https://www.justwatch.com/us/search?q=${encode(query)}`;
}

function youtubeSearch(query: string): string {
  return `https://www.youtube.com/results?search_query=${encode(query)}`;
}

function normalizeTitle(title: string | null | undefined): string {
  return (title ?? '').trim().replace(/\s+/g, ' ');
}

function appendReleaseYear(title: string, releaseDate: Date | string | null | undefined): string {
  const year = releaseYear(releaseDate);
  return year ? `${title} ${year}` : title;
}

function releaseYear(value: Date | string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : String(value.getFullYear());
  }

  const yearMatch = /^(\d{4})/.exec(value);
  if (yearMatch?.[1]) {
    return yearMatch[1];
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : String(parsed.getFullYear());
}

function encode(value: string): string {
  return encodeURIComponent(value);
}
