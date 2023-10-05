<script lang="ts" setup>
import { useGameStore } from '~/store/games';
import GameForm from './Game/Form.vue';
import MyGameForm from './Game/MyForm.vue';
import { Game, MyGame } from '#imports';
const props = defineProps({
  game: Object as PropType<Game | MyGame>,
  form: {
    type: String as PropType<"media" | "myMedia">,
    required: true
  },
  id: Number
})

const type = props.form ?? "media"
const store = useGameStore()
const forms = {
  media: GameForm,
  myMedia: MyGameForm
}

let propGame = store.Id == "Games" ? new Game() : new MyGame(props?.id ?? 0)
</script>
<template>
  <UIModalMedia>
    <template v-slot:header>
      Add Game
    </template>
    <template v-slot="scope">
      <component v-if="game" :is="forms[type]" :media="game" :show-delete="true" @exit="scope.exit" />
      <component v-else :is="forms[type]" :media="propGame" @exit="scope.exit" />
    </template>
  </UIModalMedia>
</template>
  