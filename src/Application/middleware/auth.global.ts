import { useUserStore } from "~/store/user"
export default defineNuxtRouteMiddleware(async (to, from) => {
    const store = useUserStore()
    try{
        await GetStatus()
    }catch(e: any){
        store.loggedIn = false;
    }
    if(to.path == '/Auth/Login' || to.path == '/Auth/Create')
        return
    if(!store.loggedIn){
        return navigateTo('/Auth/Login')
    }
})