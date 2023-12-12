<script setup lang="ts">
import { Creds } from '~/utils/model/user';
import { useUserStore } from '~/store/user';
const ionRouter = useIonRouter();
const Store = useUserStore()
const Credentials = ref(new Creds())
const submitted = ref(false)
Credentials.value.userName = "admin@example.com"
Credentials.value.password = "Admin123*"

async function post() {
    try {
        await Store.Login(Credentials.value)
        ionRouter.push('/Home')
    }
    catch (e) {
        submitted.value = true
    }
}
</script>
<template>
    <FormKit class="space-y-6" type="form" id="Sign in" :form-class="submitted ? 'hide' : 'show'" submit-label="Sign in"
        @submit="post" :actions="false" #default="{ value }">
        <div>
            <label for="userName" class="block text-sm font-medium leading-6 text-white">Username</label>
            <div class="mt-2">
                <input id="userName" v-model="Credentials.userName" name="userName" required
                    class="block w-full rounded-md border-0 py-1.5 text-white shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600 sm:text-sm sm:leading-6">
            </div>
        </div>
        <div>
            <div class="flex items-center justify-between">
                <label for="password" class="block text-sm font-medium leading-6 white">Password</label>
                <div class="text-sm">
                    <a href="#" class="font-semibold text-indigo-600 hover:text-indigo-500">Forgot password?</a>
                </div>
            </div>
            <div class="mt-2">
                <input id="password" v-model="Credentials.password" name="password" type="password"
                    autocomplete="current-password" required
                    class="block w-full rounded-md border-0 py-1.5 text-white shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600 sm:text-sm sm:leading-6">
            </div>
        </div>

        <div>
            <FormKit type="submit" label="Sign in" />
        </div>
    </FormKit>
    <ion-button size="small" router-link="/Auth/Create" class="float-right">Create new account</ion-button>
</template>