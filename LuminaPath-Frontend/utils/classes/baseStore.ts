import { IBasicInfo } from "../interfaces/iBasicInfo";
export function BaseStore<T extends IBasicInfo>(prefix: string) {
  const MediaList: Ref<T[] | null> = ref(null);
  const PageIndex: Ref<number> = ref(1);
  const TotalPages: Ref<number> = ref(1);
  const Prefix: string = prefix;
  const Media = computed(() => {
    return MediaList.value;
  });

  const api = {
    getMedia: async (mediaFilter?: MediaFilter): Promise<T[]> => {
      let response = await fetchMedia<T>(Prefix);
      if (typeof response === "object" && response != null)
        MediaList.value = response;
      return response;
    },

    getMediaAll: async (count?: number): Promise<T[]> => {
      let response = await fetchMediaAll<T>(count ?? 100, Prefix);
      if (typeof response === "object" && response != null)
        MediaList.value = response;
      return response;
    },

    getPaginatedMedia: async (mediaFilter: MediaFilter): Promise<T[]> => {
      let response = await fetchPaginatedMedia<T>(Prefix, mediaFilter);
      if (typeof response === "object" && response != null)
        MediaList.value = response.data;
      PageIndex.value = response.currentPage;
      TotalPages.value = response.pages;
      return response.data;
    },

    getMediaById: async (id: number): Promise<T | undefined> => {
      return await fetchMediaById<T>(id, Prefix);
    },

    removeMedia: async (id: number) => {
      await deleteMedia(id, Prefix);
      await api.getMedia();
      return;
    },

    createMedia: async (media: T) => {
      if (media.id == 0) {
        await PostMedia(media, Prefix);
      } else {
        await PutMedia(media, Prefix);
      }
      await api.getMedia();
      return;
    },
  };
  return {
    Media,
    PageIndex,
    TotalPages,
    api,
  };
}
