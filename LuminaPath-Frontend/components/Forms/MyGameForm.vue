<script setup lang="ts">
import { useGameStore } from "~/store/games"
import { MyGame } from "~/utils/model/games";

const emit = defineEmits(['exit'])
const props = defineProps({
  game: Object as PropType<MyGame>,
  showDelete: Boolean
})
async function deleteGame() {
  await store.Api.removeMedia(game.value.id)
  emit('exit')
  router.back()
}
const store = useGameStore()
const router = useIonRouter()
const game: any = ref(props.game)
const submitted = ref(false)
const gamesSelect: any = []

for (let i = 0; i < store.Media.length; i++) {
  gamesSelect.push(
    { label: store.Media[i].name, value: store.Media[i].id }
  )
}



async function confirm() {
  try {
    await store.Api.createMedia(game.value)
    await presentToast("Success!", 'primary')
    emit('exit')
  } catch (e: any) {
    await presentToast(e, 'danger')
    submitted.value = true
  }

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