import { IBasicInfo } from "../interfaces/iBasicInfo";
export function BaseStore<T extends IBasicInfo>(prefix: string) {
  const MediaList: Ref<T[] | null> = ref(null);
  const PageIndex: Ref<number> = ref(1);
  const TotalPages: Ref<number> = ref(1);
  const Prefix: string = prefix;

  const Media = computed(() => {
    return MediaList.value;
  });

  async function getMedia(mediaFilter?: MediaFilter): Promise<T[]> {
    let response = await fetchMedia<T>(Prefix);
    if (typeof response === "object" && response != null)
      MediaList.value = response;
    return response;
  }

  async function getMediaAll(count?: number): Promise<T[]> {
    let response = await fetchMediaAll<T>(count ?? 100, Prefix);
    if (typeof response === "object" && response != null)
      MediaList.value = response;
    return response;
  }

  async function getPaginatedMedia(mediaFilter: MediaFilter): Promise<T[]> {
    let response = await fetchPaginatedMedia<T>(Prefix, mediaFilter );
    if (typeof response === "object" && response != null)
      MediaList.value = response.data;
    PageIndex.value = response.currentPage;
    TotalPages.value = response.pages;
    return response.data;
  }

  async function getMediaById(id: number): Promise<T | undefined> {
    return await fetchMediaById<T>(id, Prefix);
  }

  async function removeMedia(id: number) {
    await deleteMedia(id, Prefix);
    await getMedia();
    return;
  }

  async function createMedia(media: T) {
    if (media.id == 0) {
      await PostMedia(media, Prefix);
    } else {
      await PutMedia(media, Prefix);
    }
    await getMedia();
    return;
  }
  return {
    Media,
    PageIndex,
    TotalPages,
    getMedia,
    getMediaAll,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  };
}
