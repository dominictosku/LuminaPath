import axios from 'axios'
import { IGame } from './games'
import { type Credentials } from "~/utils/user";
import { useUseStore } from "~/stores/user";

const runtimeConfig = useRuntimeConfig()

const url = runtimeConfig.public.API_ENDPOINT + "/api"
const userStore = useUseStore()

export async function fetchGames(): Promise<Array<IGame>> {
    const result: IGame[] = await $fetch<IGame[]>(url + "/games")
    return result
}

export async function deleteGame(id: number){
    await axios.delete(url + "/games?id=" + id, userStore.config)
    return
}

export async function PostGame(game: IGame){
    console.log(userStore.config)
    await axios.post(url + "/games", game, userStore.config)
    return
}

export async function LoginUser(Credentials: Credentials){
    const { data } = await axios.post(url + "/LuminaUser/BearerToken", Credentials)
    return data.token
}

export async function CreateUser(Credentials: Credentials){
    await axios.post(url + "/LuminaUser", Credentials)
}