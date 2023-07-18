<script setup lang="ts">
import { type Ref, ref, onMounted } from 'vue';
import Loading from './Loading.vue'
import { getGames } from '@/services/request';
import { type Game } from "@/utils/models/games";
import { useGameStore } from '../stores/games'

const store = useGameStore()
const games: Ref<Array<Game> | null> = ref(null);
const IsLoading = ref(true)

onMounted(async () => {
  const { data, error } = await useAsyncData('games', () => getGames())
  games.value = data.value 
  IsLoading.value = false
  console.log(games.value)
})

const isGrid = ref(false)
defineProps({
  name: String,
});

function changeIsGrid() {
  isGrid.value = !isGrid.value
}
</script>

<template>
  <MediaNav />
  <MediaTabs @changebool="changeIsGrid" />
  <div v-if="!IsLoading">
    <!-- Table view -->
    <div v-if="!isGrid" id="Table" class="tabcontent">
      <MediaTable />
    </div>
    <!-- Gallery view -->
    <div v-else id="Grid" class="tabcontent">
      <MediaGrid />
    </div>
  </div>
  <div v-else>
    <Loading />
  </div>
</template>
  
<style scoped>
/* Style the tab content */
.tabcontent {
  padding: 6px 12px;
  -webkit-animation: fadeEffect 1s;
  animation: fadeEffect 1s;
}

/* Fade in tabs */
@-webkit-keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}
</style>
  