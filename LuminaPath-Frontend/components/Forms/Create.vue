<script lang="ts" setup>
import { useGameStore } from '~/store/games';
import GameForm from './GameForm.vue';
import MyGameForm from './MyGameForm.vue';
const props = defineProps({
  game: Object,
  form: {
    type: String as PropType<"media" | "myMedia">,
    required: true
  }
})

const type = props.form ?? "media"
const store = useGameStore()
const forms = {
  media: GameForm,
  myMedia: MyGameForm 
}

let propGame = store.Id == "Games" ? new Game() : new MyGame(props?.game?.id ?? 0)
</script>
<template>
<modal-media>
  <template v-slot:header>
    Add Game
  </template>
  <template v-slot="scope">
    <component v-if="game" :is="forms[type]" :game="game" :show-delete="true" @exit="scope.exit"/>
    <component v-else :is="forms[type]" :game="propGame" @exit="scope.exit" />
  </template>
</modal-media>
</template>
  