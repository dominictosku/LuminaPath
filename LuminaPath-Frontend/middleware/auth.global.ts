import { useUserStore } from "~/stores/user"
export default defineNuxtRouteMiddleware((to, from) => {
    const store = useUserStore()
    if(to.path == '/Auth/Login' || to.path == '/Auth/Create')
        return
    if(!store.loggedIn){
        return navigateTo('/Auth/Login')
    }
})