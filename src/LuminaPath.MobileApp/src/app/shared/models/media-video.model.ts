export enum MediaVideoKind {
  Clip = 0,
  Guide = 1,
  SceneClip = 2,
}

export type MediaVideoContext = 'games' | 'animes' | 'series' | 'movies';

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
