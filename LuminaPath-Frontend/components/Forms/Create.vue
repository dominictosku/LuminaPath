<script lang="ts" setup>
import { useGameStore } from '~/store/games';
defineProps({
  game: Object,
})

const store = useGameStore()
let newMyGame = new MyGame(0, 0, new Date(), new Date(), 0, 0, store.SelectedGameId)
let newGame = new Game(0, "", "", "", 0, 0)
let propGame = store.Id == "Games" ? newGame : newMyGame
</script>
<template>
<modal-media>
  <template v-slot:header>
    Add Game
  </template>
  <template v-slot="scope">
    <component v-if="game" :is="store.ActiveComponent.Forms" :game="game" :show-delete="true" @exit="scope.exit"/>
    <component v-else :is="store.ActiveComponent.Forms" :game="propGame" @exit="scope.exit" />
  </template>
</modal-media>
</template>
  