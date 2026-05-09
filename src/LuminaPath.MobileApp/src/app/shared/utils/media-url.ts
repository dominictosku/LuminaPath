import { resolveApiEndpoint } from 'src/app/shared/services/api-endpoint.service';
import { MediaFile } from 'src/app/features/library/models/mediaFile.model';

const placeholderImage = 'assets/png/Placeholder.png';

export function mediaImageUrl(image: MediaFile | null | undefined): string {
  const source = image?.uri ?? image?.url;

  if (!source) {
    return placeholderImage;
  }

  if (source.startsWith('http://') || source.startsWith('https://') || source.startsWith('data:') || source.startsWith('assets/')) {
    return source;
  }

  const apiEndpoint = resolveApiEndpoint().replace(/\/$/, '');
  const apiOrigin = apiEndpoint.replace(/\/api$/, '');
  const normalizedSource = source.replace(/^\//, '');

  if (normalizedSource.startsWith('api/')) {
    return `${apiOrigin}/${normalizedSource}`;
  }

  return `${apiEndpoint}/${normalizedSource}`;
}
