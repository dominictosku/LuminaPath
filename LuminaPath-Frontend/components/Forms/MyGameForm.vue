<script setup lang="ts">
import { useMyGameStore } from "~/store/myGames"
import { useGameStore } from "~/store/games"
import { MyGame } from "~/utils/games";

const emit = defineEmits(['exit'])
const props = defineProps({
  game: Object as PropType<MyGame>,
  showDelete: Boolean
})
async function deleteGame() {
  await store.removeMedia(game.value.id)
  emit('exit')
  router.back()
}
const store = useMyGameStore()
const gameStore = useGameStore()
const { data: games } = await useAsyncData('games', () => gameStore.getMedia())
const router = useIonRouter()
const game: any = ref(props.game)
const submitted = ref(false)
const gamesSelect: any = []
if(games.value){
    for(let i = 0; i < games.value.length; i++){
        gamesSelect.push(
            { label: games.value[i].name, value: games.value[i].id }
        )
    }
}


async function confirm() {
  try {
    await store.createMedia(game.value)
    await presentToast("Success!", 'primary')
    emit('exit')
  } catch (e : any) {
    await presentToast(e, 'danger')
    submitted.value = true
  }

}
</script>
<template>
  <FormKit class="" type="form" id="edit" :form-class="submitted ? 'hide' : 'show'" submit-label="Confirm" @submit="confirm"
    :actions="false" #default="{ value }">
    <div class="grid justify-center">
      <FormKit v-model="game.gameId" type="select" name="gamesSelect" label="Game" :options="gamesSelect" />
      <FormKit v-model="game.rating" type="number" name="rating" label="Rating" />
      <FormKit v-model="game.startDate" type="date" label="Start date" />
      <FormKit v-model="game.timeSpend" type="number" name="playtime" label="Your Playtime" step="1" />
      <div class="flex justify-end p-3 gap-3">
        <IonButton class="h-12" v-if="showDelete" @click="deleteGame()" color="danger">delete</IonButton>
        <IonButton class="h-12" @click="emit('exit')" color="light">close</IonButton>
        <FormKit type="submit" label="Confirm" />
      </div>
    </div>
  </FormKit>
</template>