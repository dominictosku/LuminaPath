<script setup lang="ts">
import { useGameStore } from '~/store/games';

const store = useGameStore()
const countMedia = ref(50)
await useAsyncData(`${store.Id}${countMedia.value}`, () => store.Api.getMediaAll(store.Id, countMedia.value))
const ionInfinite = async (ev: any) => {
    countMedia.value += 50;
    await store.Api.getMediaAll(store.Id, countMedia.value)
    setTimeout(() => {
        ev.target.complete();
    }, 500);
};

</script>

<template>
    <ol role="list" class="grid md:grid-cols-4 grid-cols-2 gap-4">
        <li v-for="media in store.Media" v-bind:key="media.id" style="--i: 2; --length: 10">
            <MediaGridData :game="media" />
        </li>
    </ol>
    <ion-infinite-scroll @ionInfinite="ionInfinite">
        <ion-infinite-scroll-content></ion-infinite-scroll-content>
    </ion-infinite-scroll>
</template>

<style scoped>
* {
    box-sizing: border-box;
}

body {
    --h: 212deg;
    --l: 43%;
    --brandColor: hsl(var(--h), 71%, var(--l));
    font-family: Montserrat, sans-serif;
    margin: 0;
    background-color: whitesmoke;
}

p {
    margin: 0;
    line-height: 1.6;
}

ol {
    list-style: none;
    counter-reset: list;
    padding: 0 1rem;
}

li {
    --stop: calc(100% / var(--length) * var(--i));
    --l: 62%;
    --l2: 88%;
    --h: calc((var(--i) - 1) * (180 / var(--length)));
    --c1: hsl(var(--h), 71%, var(--l));
    --c2: hsl(var(--h), 71%, var(--l2));

    position: relative;
    counter-increment: list;
    max-width: 30rem;
    margin: 2rem auto;
    padding: 2rem 1rem 1rem;
    box-shadow: 0.1rem 0.1rem 1.5rem rgba(0, 0, 0, 0.3);
    border-radius: 0.25rem;
    overflow: hidden;
    background-color: white;
}

li::before {
    content: '';
    display: block;
    width: 100%;
    height: 1rem;
    position: absolute;
    top: 0;
    left: 0;
    background: linear-gradient(to right, var(--c1) var(--stop), var(--c2) var(--stop));
}

h3 {
    display: flex;
    align-items: baseline;
    margin: 0 0 1rem;
    color: rgb(70 70 70);
}

h3::before {
    display: flex;
    justify-content: center;
    align-items: center;
    flex: 0 0 auto;
    margin-right: 1rem;
    width: 3rem;
    height: 3rem;
    content: counter(list);
    padding: 1rem;
    border-radius: 50%;
    background-color: var(--c1);
    color: white;
}

@media (min-width: 40em) {
    li {
        margin: 3rem auto;
        padding: 3rem 2rem 2rem;
    }

    h3 {
        font-size: 2.25rem;
        margin: 0 0 2rem;
    }

    h3::before {
        margin-right: 1.5rem;
    }
}
</style>