<script setup lang="ts">
import { Creds } from '~/utils/model/user';
import { useUserStore } from '~/store/user';

const ionRouter = useIonRouter();
const Store = useUserStore()
const Credentials = ref(new Creds())
const submitted = ref(false)

async function post() {
    try {
        await Store.Create(Credentials.value)
        ionRouter.push('/Auth/Login')
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
            <label for="userName" class="block text-sm font-medium leading-6 text-white">Email</label>
            <div class="mt-2">
                <input id="Email" v-model="Credentials.email" name="Email" required
                    class="block w-full rounded-md border-0 py-1.5 text-white shadow-sm ring-1 ring-inset ring-gray-300 placeholder:text-gray-400 focus:ring-2 focus:ring-inset focus:ring-indigo-600 sm:text-sm sm:leading-6">
            </div>
        </div>
        <div>
            <div class="mt-2">
                <FormKit v-model="Credentials.password" type="password" name="password" label="Password"
                    validation="required|length:6|matches:/[^a-zA-Z]/" :validation-messages="{
                        matches: 'Please include at least one symbol',
                    }" placeholder="Your password" help="Choose a password" />
                <FormKit type="password" name="password_confirm" label="Confirm password" placeholder="Confirm password"
                    validation="required|confirm" help="Confirm your password" />
            </div>
        </div>

        <div>
            <FormKit type="submit" label="Create account" />
        </div>
    </FormKit>
</template>