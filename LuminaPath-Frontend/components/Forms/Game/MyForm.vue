<script setup lang="ts">
import { useGameStore } from "~/store/games"
import { Game, MyGame } from "~/utils/model/games";

const emit = defineEmits(['exit'])
const props = defineProps({
  media: {
    type: Object,
    required: true
  },
  showDelete: Boolean
})


const store = useGameStore()
const router = useIonRouter()
const game = ref(props.media as MyGame)
const submitted = ref(false)
const gamesSelect: any = []

const { data, error } = await useAsyncData(game.value.gameId.toString(), () => 
  fetchMediaById<Game>(game.value.gameId, "games"))



gamesSelect.push(
  { label: data.value?.name, value: data.value?.id }
)

async function confirm() {
  try {
    console.log(game.value)
    await store.Api.createMedia(game.value, store.Type.MyGames)
    await presentToast("Success!", 'primary')
    emit('exit')
  } catch (e: any) {
    await presentToast(e, 'danger')
    submitted.value = true
  }

}
async function deleteGame() {
  await store.Api.removeMedia(game.value.id, store.Type.MyGames)
  emit('exit')
  router.back()
}
</script>
<template>
  <FormKit class="" type="form" id="edit" :form-class="submitted ? 'hide' : 'show'" submit-label="Confirm"
    @submit="confirm" :actions="false" #default="{ value }">
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