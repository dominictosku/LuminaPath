export enum MediaVideoKind {
  Clip = 0,
  Guide = 1,
  SceneClip = 2,
}

export type MediaVideoContext = 'games' | 'animes' | 'series' | 'movies';

export interface MediaVideoKindOption {
  label: string;
  value: MediaVideoKind;
}

export interface MediaVideoDraft {
  title: string;
  description: string;
  kind: MediaVideoKind;
  file: File;
}

export interface MediaVideo {
  id: number;
  mediaId: number;
  title: string;
  description: string | null;
  kind: MediaVideoKind;
  sortOrder: number;
  fileName: string;
  storageName: string;
  contentType: string;
  sizeBytes: number;
  durationSeconds: number | null;
  createdAt: string;
  updatedAt: string | null;
  streamUrl?: string;
}

export interface MediaVideoUpload {
  mediaId: number;
  title: string;
  description: string;
  kind: MediaVideoKind;
  file: File;
}

export interface MediaVideoUpdate {
  title: string;
  description: string | null;
  kind: MediaVideoKind;
}

export function mediaVideoKindOptions(context: MediaVideoContext): MediaVideoKindOption[] {
  if (context === 'games') {
    return [
      { label: 'Clip', value: MediaVideoKind.Clip },
      { label: 'Guide', value: MediaVideoKind.Guide },
    ];
  }

  return [
    { label: 'Scene clip', value: MediaVideoKind.SceneClip },
  ];
}

export function defaultMediaVideoKind(context: MediaVideoContext): MediaVideoKind {
  return context === 'games' ? MediaVideoKind.Clip : MediaVideoKind.SceneClip;
}

export function mediaVideoKindLabel(kind: MediaVideoKind, context: MediaVideoContext): string {
  return mediaVideoKindOptions(context).find((option) => option.value === kind)?.label
    ?? (kind === MediaVideoKind.Guide ? 'Guide' : kind === MediaVideoKind.SceneClip ? 'Scene clip' : 'Clip');
}

export function formatMediaVideoFileSize(sizeBytes: number): string {
  if (!Number.isFinite(sizeBytes) || sizeBytes <= 0) {
    return 'Unknown size';
  }

  const megabytes = sizeBytes / 1024 / 1024;
  if (megabytes >= 1) {
    return `${Math.round(megabytes * 10) / 10} MB`;
  }

  return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
}
