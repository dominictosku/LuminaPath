import axios from "axios";
import { IGame } from "./games";
import { type Credentials } from "~/utils/user";
import { useUserStore } from "~/stores/user";

const getApiUrl = (endpoint: string) => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT + "/api" + endpoint;
};

const apiCall = async (url: string, data: any, config: any) => {
  const userStore = useUserStore();
  try {
    return await axios.post(url, data, { ...userStore.config, ...config });
  } catch (error) {
    // Handle error if needed
    console.error("API call failed:", error);
    throw error;
  }
};

export async function fetchGames(): Promise<Array<IGame>> {
  const url = getApiUrl("/games");
  const result: IGame[] = await $fetch<IGame[]>(url);
  return result;
}

export async function fetchGameById(id: number): Promise<IGame> {
  const url = getApiUrl("/games/" + id);
  const result: IGame = await $fetch<IGame>(url);
  return result;
}

export async function deleteGame(id: number) {
  const url = getApiUrl("/games?id=" + id);
  await apiCall(url, null, {});
}

export async function PostGame(game: IGame) {
  const url = getApiUrl("/games");
  await apiCall(url, game, {});
}

export async function LoginUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser/BearerToken");
  const { data } = await apiCall(url, Credentials, {});
  return data.token;
}

export async function CreateUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser");
  await apiCall(url, Credentials, {});
}
