<script setup lang="ts">
import { useGameStore } from "~/store/games"
import { Game } from "~/utils/model/games";

const emit = defineEmits(['exit'])

const props = defineProps({
  game: Object as PropType<Game>,
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
  <FormKit class="" type="form" id="edit" :form-class="submitted ? 'hide' : 'show'" submit-label="Confirm" @submit="confirm"
    :actions="false" #default="{ value }">
    <div class="grid justify-center">
      <FormKit v-model="game.name" name="name" label="Title of game" validation="required" />
      <FormKit v-model="game.description" type="textarea" name="description" label="description" />
      <FormKit v-model="game.plattforms" type="select" name="plattforms" label="Plattform" placeholder="Playstation"
        :options="Plattforms" />
      <FormKit v-model="game.genre" type="text" name="genre" label="genre" />
      <FormKit v-model="game.releaseDate" type="date" label="Release date" />
      <FormKit v-model="game.playtime" type="number" name="playtime" label="Estimated Playtime" step="1" />
      <div class="flex justify-end p-3 gap-3">
        <IonButton class="h-12" v-if="showDelete" @click="deleteGame()" color="danger">delete</IonButton>
        <IonButton class="h-12" @click="emit('exit')" color="light">close</IonButton>
        <FormKit type="submit" label="Confirm" />
      </div>
    </div>
  </FormKit>
</template>