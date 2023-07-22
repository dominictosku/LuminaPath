import { useUseStore } from "~/stores/user"
export default defineNuxtRouteMiddleware((to, from) => {
    const store = useUseStore()
    if(to.path == '/Auth/Login' || to.path == '/Auth/Create')
        return
    if(!store.loggedIn){
        return navigateTo('/Auth/Login')
    }
})