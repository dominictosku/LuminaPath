<script setup lang="ts">
const titles = ["Games", "Anime", "Movies/Series"]
</script>
<template>
    <div class="grid grid-cols-1 md:grid-cols-2 p-12 gap-4">
        <article class="p-6 bg-white rounded-lg border border-gray-200 shadow-md dark:bg-gray-800 dark:border-gray-700">
            <h2 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                <p class="text-blue-500">
                    Beginn Your Journey
                </p>
            </h2>
            <p class="mb-5 font-light text-gray-500 dark:text-gray-400">
                Keep tabs on your gaming adventures. Monitor your anime watching progress.
                Keep an eye on your favorite series and movies, all conveniently in one place!
            </p>
            <div class="flex justify-between items-center">
                <a href="#"
                    class="inline-flex items-center font-medium text-primary-600 dark:text-primary-500 hover:underline">
                    Read more
                    <svg class="ml-2 w-4 h-4" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
                        <path fill-rule="evenodd"
                            d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z"
                            clip-rule="evenodd"></path>
                    </svg>
                </a>
            </div>
        </article>
        <div class="h-fit rounded">
            <ul>
                <li v-for="title in titles" class="bg-slate-600 rounded text-center cursor-pointer linkTab">
                    <p class="bg-indigo-600">
                    {{ title }}
                    </p>
                </li>
            </ul>
        </div>
    </div>
</template>
<style scoped>
.linkTab {
    width: 100%;
    height: 4em;
    background-image: url("https://upload.wikimedia.org/wikipedia/commons/thumb/3/3f/Placeholder_view_vector.svg/1362px-Placeholder_view_vector.svg.png");
    background-repeat: no-repeat;
    background-size: cover;
    background-position: center;
    -webkit-transition: all .5s;
    -moz-transition: all .5s;
    -o-transition: all .5s;
    transition: all .5s;
}

.linkTab:hover {
    -ms-transform: scale(1.2);
    -moz-transform: scale(1.2);
    -webkit-transform: scale(1.2);
    -o-transform: scale(1.2);
    transform: scale(1.2);
}
</style>