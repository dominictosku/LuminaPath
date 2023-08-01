import axios from "axios";
import { IGame } from "./games";
import { type Credentials } from "~/utils/user";
import { useUserStore } from "~/stores/user";

const getApiUrl = (endpoint: string) => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT + "/api" + endpoint;
};

const apiCall = async (url: string, data: any) => {
  const userStore = useUserStore();
  try {
    return await axios.post(url, data, {
      withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
    },
  });
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
  await axios.delete(url, {
    withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
  });
}

export async function PostGame(game: IGame) {
  const url = getApiUrl("/games");
  await apiCall(url, game);
}

export async function LoginUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser/BearerToken");
  const { data } = await apiCall(url, Credentials);
  return 'data.token';
}

export async function CreateUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser");
  await apiCall(url, Credentials);
}
