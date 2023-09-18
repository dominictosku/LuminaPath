<script lang="ts" setup>
import Games from "./GameForm.vue"
import MyGames from "./MyGameForm.vue"
const props = defineProps({
  game: Object,
  gameId: Number,
  formType: String
})

const forms: any = {
  Games,
  MyGames
}
let newGame = new Game(0, "", "", "", 0, 0)
let newMyGame = new MyGame(0, 0, new Date(), new Date(), 0, 0, props.gameId ?? 0)
let propGame = props.formType == "Games" ? newGame : newMyGame
</script>
<template>
<modal-media>
  <template v-slot:header>
    Add Game
  </template>
  <template v-slot="scope">
    <component v-if="game" :is="forms[formType ?? 'Games']" :game="game" :show-delete="true" @exit="scope.exit"/>
    <component v-else :is="forms[formType ?? 'Games']" :game="propGame" @exit="scope.exit" />
  </template>
</modal-media>
</template>
  