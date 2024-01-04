<script setup lang="ts">
import { useGameStore } from '~/store/games';

const store = useGameStore()
const countMedia = ref(50)
store.Filter.setCount(countMedia.value)
await useAsyncData(`${store.Id}${countMedia.value}`, () => store.Api.getMedia())
const ionInfinite = async (ev: any) => {
    countMedia.value += 50;
    store.Filter.setCount(countMedia.value)
    await store.Api.getMedia();
    setTimeout(() => {
        ev.target.complete();
    }, 500);
};

const GroupedMedia = computed(() => {
    const media = store.Media;
    return media.reduce((result: any, item) => {
        const date = new Date(item.releaseDate);
        const month = date.getMonth() + 1; // Months are zero-based, so we add 1 to get the actual month.
        const year = date.getFullYear();
        const key = `${year}-${month}`;

        if (!result[key]) {
            result[key] = [];
        }

        result[key].push(item);
        return result;
    }, {});
})

const sortedKeys = computed(() => {
    return Object.keys(GroupedMedia.value).sort((a, b) => {
        // Convert the keys (in the format "MM-YYYY") to date objects for comparison
        const dateA: any = new Date(b);
        const dateB: any = new Date(a);
        return dateA - dateB;
    });
})

function formatMonthYear(dateString: string) {
    const [year, month] = dateString.split('-');
    const monthNames = [
        'January', 'February', 'March', 'April',
        'May', 'June', 'July', 'August',
        'September', 'October', 'November', 'December'
    ];

    const formattedDate = `${monthNames[parseInt(month) - 1]} ${year}`;
    return formattedDate;
}

</script>

<template>
    <div v-for="key in sortedKeys" :key="key">
        <h2 class="px-12 w-full bg-slate-600">{{ formatMonthYear(key) }}</h2>
        <ol role="list" class="grid sm:grid-cols-4 grid-cols-2 gap-2">
            <li class="bg-slate-800" v-for="media in GroupedMedia[key]" :key="media.id" style="--i: 2; --length: 10">
                <MediaGridData :game="media" />
            </li>
        </ol>
    </div>
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
    margin: 2rem auto;
    padding: 2rem 1rem 1rem;
    box-shadow: 0.1rem 0.1rem 1.5rem rgba(0, 0, 0, 0.3);
    border-radius: 0.25rem;
    overflow: hidden;
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
</style>