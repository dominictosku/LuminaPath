<script lang="ts" setup>
import GameForms from "./GameForm.vue"
import MyGameForms from "./MyGameForm.vue"
const props = defineProps({
  formType: String
})

const forms: any = {
  GameForms,
  MyGameForms
}
let newGame = new Game(0, "", "", "", 0, 0)
let newMyGame = new MyGame(0, 0, new Date(), new Date(), 0, 0, 1)
let propGame = props.formType == "GameForms" ? newGame : newMyGame
</script>
<template>
<modal-media>
  <template v-slot:header>
    Add Game
  </template>
  <template v-slot="scope">
    <component :is="forms[formType ?? 'GameForms']" :game="propGame" @exit="scope.exit" />
  </template>
</modal-media>
</template>
  