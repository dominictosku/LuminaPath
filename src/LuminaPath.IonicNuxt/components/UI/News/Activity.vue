<script setup lang="ts">
const router = useIonRouter()
</script>
<template>
    <div class="flex flex-col items-center NewsCard">
        <img class="object-cover w-full rounded-t-lg h-48 md:h-auto md:w-48 md:rounded-none md:rounded-l-lg"
            src="https://images.pexels.com/photos/956999/milky-way-starry-sky-night-sky-star-956999.jpeg" alt="">
        <div class="flex flex-col justify-between p-4 leading-normal">
            <h5 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                New Games this month
            </h5>
            <p class="mb-3 font-normal text-gray-700 dark:text-gray-400">
                Track your games and share progress with friends
            </p>
            <ion-button @click="router.push('/MediaList/Games')">Games</ion-button>
        </div>
    </div>
</template>
<style scoped>
.NewsCard {
  @apply bg-white border border-gray-200 rounded-lg shadow md:flex-row md:max-w-xl hover:bg-gray-100 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700
}
</style>