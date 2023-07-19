import axios from 'axios'
import { IGame } from 'utils/models/games'
const runtimeConfig = useRuntimeConfig()

const url = runtimeConfig.public.API_ENDPOINT

export async function fetchGames(): Promise<Array<IGame>> {
    const result: IGame[] = await $fetch<IGame[]>(url + "/games")
    return result
}

export async function PostGame(game: IGame){
    await axios.post(url + "/games", game)
    return
}