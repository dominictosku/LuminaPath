<script setup lang="ts">
import Games from "./GameForm.vue"
import MyGames from "./MyGameForm.vue"
defineProps({
  game: Object,
  formType: String
  })
  
  const forms: any = {
    Games,
    MyGames
  }
</script>
<template>
<modal-media>
  <template v-slot:header>
    Edit Game
  </template>
  <template v-slot="scope">
    <component :is="forms[formType ?? 'Games']" :game="game" :show-delete="true" @exit="scope.exit"/>
  </template>
</modal-media>
</template>
  