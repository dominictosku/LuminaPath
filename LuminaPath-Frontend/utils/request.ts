import axios from 'axios'
import { IGame } from './games'
import { type Credentials } from "~/utils/user";
import { useUseStore } from "~/stores/user";

export async function fetchGames(): Promise<Array<IGame>> {
    const runtimeConfig = useRuntimeConfig()
    const url = runtimeConfig.public.API_ENDPOINT + "/api"
    const result: IGame[] = await $fetch<IGame[]>(url + "/games")
    return result
}

export async function deleteGame(id: number){
    const userStore = useUseStore()
    const runtimeConfig = useRuntimeConfig()
    const url = runtimeConfig.public.API_ENDPOINT + "/api"
    await axios.delete(url + "/games?id=" + id, userStore.config)
    return
}

export async function PostGame(game: IGame){
    const userStore = useUseStore()
    const runtimeConfig = useRuntimeConfig()
    const url = runtimeConfig.public.API_ENDPOINT + "/api"
    console.log(userStore.config)
    await axios.post(url + "/games", game, userStore.config)
    return
}

export async function LoginUser(Credentials: Credentials){
    const runtimeConfig = useRuntimeConfig()
    const url = runtimeConfig.public.API_ENDPOINT + "/api"
    const { data } = await axios.post(url + "/LuminaUser/BearerToken", Credentials)
    return data.token
}

export async function CreateUser(Credentials: Credentials){
    const runtimeConfig = useRuntimeConfig()
    const url = runtimeConfig.public.API_ENDPOINT + "/api"
    await axios.post(url + "/LuminaUser", Credentials)
}