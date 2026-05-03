import { environment } from 'src/environments/environment';
import { MediaFile } from 'src/app/features/media/models/mediaFile.model';

const placeholderImage = 'assets/png/Placeholder.png';

export function mediaImageUrl(image: MediaFile | null | undefined): string {
  const source = image?.uri ?? image?.url;

  if (!source) {
    return placeholderImage;
  }

  if (source.startsWith('http://') || source.startsWith('https://') || source.startsWith('data:') || source.startsWith('assets/')) {
    return source;
  }

  const apiEndpoint = environment.endpoint.replace(/\/$/, '');
  const apiOrigin = apiEndpoint.replace(/\/api$/, '');
  const normalizedSource = source.replace(/^\//, '');

  if (normalizedSource.startsWith('api/')) {
    return `${apiOrigin}/${normalizedSource}`;
  }

  return `${apiEndpoint}/${normalizedSource}`;
}
