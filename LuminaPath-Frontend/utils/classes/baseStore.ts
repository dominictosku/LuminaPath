import { IApi } from "../interfaces/IApiInterface";
import { IBasicInfo } from "../interfaces/iBasicInfo";
import { MediaFilter } from "./mediaFilter";
import { PaginateResult } from "./paginatedResult";
export function BaseStore<T extends IBasicInfo>(id: string) {
  const Id : Ref<string> = ref(id)
  const NextEndpoint = ref("")
  const AddEnpoint = computed(() => {
    if(NextEndpoint.value == ""){
      return "";
    }
    return `/${NextEndpoint.value}`
  })
  const MediaList: Ref<T[]> = ref([]);
  const PageIndex: Ref<number> = ref(1);
  const TotalPages: Ref<number> = ref(1);
  const Media = computed(() => {
    return MediaList.value;
  });

const ChangeActiveValue = (value: any) => {
    MediaList.value = value
}

  const Api: IApi<IBasicInfo> = {
    getMedia: async (
      endPoint: string,
      mediaFilter?: MediaFilter
    ): Promise<PaginateResult<IBasicInfo>> => {
      let response = await fetchPaginatedMedia<IBasicInfo>(
        endPoint + AddEnpoint.value,
        mediaFilter
      );
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response.data);
      PageIndex.value = response.currentPage;
      TotalPages.value = response.pages;
      return response;
    },

    getMediaAll: async (endPoint: string, count?: number): Promise<T[]> => {
      let response = await fetchMediaAll<T>(count ?? 100, endPoint + AddEnpoint.value);
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response);
      return response;
    },

    getMediaById: async (id: number, endPoint: string): Promise<T | undefined> => {
      return await fetchMediaById<T>(id, endPoint);
    },

    removeMedia: async (id: number, endPoint: string) => {
      await deleteMedia(id, endPoint);
      await Api.getMedia(endPoint);
      return;
    },

    createMedia: async (media: IBasicInfo, endPoint: string) => {
      if (media.id == 0) {
        await PostMedia(media, endPoint);
      } else {
        await PutMedia(media, endPoint);
      }
      await Api.getMedia(Id.value);
      return;
    },
  };

  return {
    Id,
    Media,
    NextEndpoint,
    PageIndex,
    TotalPages,
    Api,
  };
}
