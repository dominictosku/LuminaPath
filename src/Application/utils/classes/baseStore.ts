import { type IApi } from "../interfaces/IApi";
import { type IBasicInfo } from "../interfaces/iBasicInfo";
import { MediaFilter } from "./mediaFilter";
import { PaginateResult } from "./paginatedResult";
import { Pagination } from "./pagination";
export function BaseStore<T extends IBasicInfo>(id: string) {
  const Id: Ref<string> = ref(id);
  const MainEndpoint = ref(id)
  const MediaList: Ref<T[]> = ref([]);
  const Paging = ref(new Pagination())
  const Media = computed(() => {
    return MediaList.value;
  });
  const Filter = ref(new MediaFilter())

  const ChangeActiveValue = (value: any) => {
    MediaList.value = value;
  };

  const Api: IApi<IBasicInfo> = {
    getMedia: async (): Promise<PaginateResult<IBasicInfo>> => {
      let response = await fetchPaginatedMedia<IBasicInfo>(
        MainEndpoint.value,
        Filter.value
      );
      if (typeof response === "object" && response != null)
        ChangeActiveValue(response.data);
      Paging.value.PageIndex = response.currentPage;
      Paging.value.TotalPages = response.pages;
      return response;
    },

    getMediaById: async (
      id: number,
      endPoint: string
    ): Promise<T | undefined> => {
      return await fetchMediaById<T>(id, endPoint);
    },

    removeMedia: async (id: number, endPoint: string) => {
      await deleteMedia(id, endPoint);
      await Api.getMedia();
      return;
    },

    createMedia: async (media: IBasicInfo, endPoint: string) => {
      if (media.id == 0) {
        await PostMedia(media, endPoint);
      } else {
        await PutMedia(media, endPoint);
      }
      await Api.getMedia();
      return;
    },
  };

  return {
    Id,
    MainEndpoint,
    Filter,
    Media,
    Paging,
    Api,
  };
}
