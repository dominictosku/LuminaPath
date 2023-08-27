import axios from "axios";
import { IGame } from "~/utils/games";
import { PaginateResult } from "~/utils/paginatedResult";
import { type Credentials } from "~/utils/user";

const getApiUrl = (endpoint: string) => {
  const runtimeConfig = useRuntimeConfig();
  return runtimeConfig.public.API_ENDPOINT + "/api" + endpoint;
};

const apiCall = async (url: string, data: any) => {
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

export async function fetchPaginatedGames(): Promise<PaginateResult> {
  const url = getApiUrl("/games");
  const result: PaginateResult = await $fetch<PaginateResult>(url);
  return result;
}

export async function fetchGames(howMany: number): Promise<Array<IGame>> {
  const url = getApiUrl(`/games/all/${howMany}`);
  const result: Array<IGame> = await $fetch<Array<IGame>>(url);
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

export async function PutGame(game: IGame) {
  const url = getApiUrl(`/games/${game.id}`);
  await axios.put(url, game, {
    withCredentials: true, // This is crucial to include the HttpOnly cookie in the request
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
  });
}

export async function LoginUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser/BearerToken");
  await apiCall(url, Credentials);
  return 'data.token';
}

export async function CreateUser(Credentials: Credentials) {
  const url = getApiUrl("/LuminaUser");
  await apiCall(url, Credentials);
}
