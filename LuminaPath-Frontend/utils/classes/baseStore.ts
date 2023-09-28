import { IApi } from "../interfaces/IApiInterface";
import { IBasicInfo } from "../interfaces/iBasicInfo";
export function BaseStore<T extends IBasicInfo, T2>(id: string) {
  const Id : Ref<string> = ref(id)
  const Endpoint = computed(() => {
    return Id.value
  })
  const MediaList: Ref<T[] | null> = ref(null);
  const MyMediaList: Ref<T2[] | null> = ref(null)
  const PageIndex: Ref<number> = ref(1);
  const TotalPages: Ref<number> = ref(1);
  const Mode: Ref<"Media" | "MyMedia"> = ref("Media")
  const Media = computed(() => {
    return MediaList.value;
  });
  const MyMedia = computed(() => {
    return MyMediaList.value;
  });

const ChangeActiveValue = (value: any) => {
  if(Mode.value == "Media"){
    MediaList.value = value
  }else{
    MyMediaList.value = value
  }
}

  const Api: IApi<IBasicInfo> = {
    getMedia: async <IBasicInfo>(mediaFilter?: MediaFilter): Promise<IBasicInfo[]> => {
      let response = await fetchMedia<IBasicInfo>(Endpoint.value);
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response);
      return response;
    },

    getMediaAll: async (count?: number): Promise<T[]> => {
      let response = await fetchMediaAll<T>(count ?? 100, Endpoint.value);
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response);
      return response;
    },

    getPaginatedMedia: async (mediaFilter: MediaFilter): Promise<T[]> => {
      let response = await fetchPaginatedMedia<T>(Endpoint.value, mediaFilter);
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response.data);
      PageIndex.value = response.currentPage;
      TotalPages.value = response.pages;
      return response.data;
    },

    getMediaById: async (id: number): Promise<T | undefined> => {
      return await fetchMediaById<T>(id, Endpoint.value);
    },

    removeMedia: async (id: number) => {
      await deleteMedia(id, Endpoint.value);
      await Api.getMedia();
      return;
    },

    createMedia: async (media: IBasicInfo) => {
      if (media.id == 0) {
        await PostMedia(media, Endpoint.value);
      } else {
        await PutMedia(media, Endpoint.value);
      }
      await Api.getMedia();
      return;
    },
  };

  return {
    Id,
    Media,
    MyMedia,
    Mode,
    PageIndex,
    TotalPages,
    Api,
  };
}
