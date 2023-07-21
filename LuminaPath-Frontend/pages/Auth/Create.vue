<template>
    <IonPage>
        <NavigationGoBack>
            Login
        </NavigationGoBack>
        <IonContent>
            <div class="flex flex-col justify-center py-12 px-6 lg:px-8">
                <div class="sm:mx-auto sm:w-full sm:max-w-sm">
                    <img class="mx-auto h-10 w-auto" src="https://tailwindui.com/img/logos/mark.svg?color=indigo&shade=600"
                        alt="Your Company">
                    <h2 class="mt-10 text-center text-2xl font-bold leading-9 tracking-tight text-white">Sign in to your
                        account</h2>
                </div>

                <div class="mt-4 sm:mx-auto sm:w-full sm:max-w-sm">
                    <FormsUserCreate/>
                </div>
            </div>
        </IonContent>
    </IonPage>
</template>