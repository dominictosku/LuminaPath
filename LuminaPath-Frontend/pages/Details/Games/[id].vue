<script setup lang="ts">
import { IGame } from '~/utils/games';
import { useRoute } from 'vue-router';
import Modal from '~/components/Forms/Edit.vue';
import { create } from 'ionicons/icons';

const route = useRoute();
const { id } = route.params;
const idInt: number = parseInt(id as string);
const { data: editGame } = await useAsyncData('games/' + idInt, async () => await fetchMediaById<IGame>(idInt, "games"))

if (!editGame) {
  throw createError({ statusCode: 404, statusMessage: 'Page Not Found' })
}
const modalProps = { game: editGame }

</script>
<template>
  <ion-page>
    <NavigationGoBack>
      <p>Edit Game: {{ id }} </p>
    </NavigationGoBack>
    <ion-content>
      <div class="grid grid-cols-2 gap-2 border-2 border-slate-500">
        <div>
          <img width="2000" height="2000" src="https://images.pexels.com/photos/956999/milky-way-starry-sky-night-sky-star-956999.jpeg" />
        </div>
        <div class="">
          <ul>
            <li class="text-xl">
              Title:
              {{ editGame?.name }}
            </li>
            <li class="text-xl">
              Genre:
              {{ editGame?.genre }}
            </li>
            <li class="text-xl">
              Plattforms:
              {{ editGame?.plattforms }}
            </li>
            <li class="text-xl">
              Estimated Playtime:
              {{ editGame?.playtime }}
            </li>
            <li class="text-xl">
              Details: 
              {{ editGame?.description }}
            </li>
          </ul>
        </div>
      </div>
      <ion-fab-button class="absolute right-2" @click="openModal(Modal, modalProps)" size="small">
        <ion-icon :icon="create"></ion-icon>
      </ion-fab-button>
    </ion-content>
  </ion-page>
</template>
  