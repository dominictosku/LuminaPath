export class MediaFile {
  id: number;
  name: string | null;
  uri: string | null;
  url: string | null;
  contentType: string | null;
  /**
   * Blob identifier on the backend. Required when sending a newly-uploaded
   * file back to the server (e.g. `Game.Image.StorageName`); the backend
   * computes `Url` from it on the way out.
   */
  storageName: string | null;
  constructor() {
    this.id = 0;
    this.name = null;
    this.uri = null;
    this.url = null;
    this.contentType = null;
    this.storageName = null;
  }
}
