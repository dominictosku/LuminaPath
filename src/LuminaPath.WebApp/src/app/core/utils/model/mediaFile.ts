export class MediaFile {
  id: number;
  name: string | null;
  uri: string | null;
  contentType: string | null;
  constructor() {
    this.id = 0;
    this.name = null;
    this.uri = null;
    this.contentType = null;
  }
}
