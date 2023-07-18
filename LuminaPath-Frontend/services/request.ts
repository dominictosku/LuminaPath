import axios from 'axios'
import { Game } from 'utils/models/games'
const runtimeConfig = useRuntimeConfig()

const url = runtimeConfig.public.API_ENDPOINT

export async function fetchGames(): Promise<Array<Game>> {
    const result = await axios.get(url + "/games")
    return result.data
}

export async function PostGame(){
    await axios.post(url + "/games")
    return
}