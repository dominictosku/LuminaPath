<script setup lang="ts">
import GameForms from "./GameForm.vue"
import MyGameForms from "./MyGameForm.vue"
defineProps({
  game: Object,
  formType: String
  })
  
  const forms: any = {
    GameForms,
    MyGameForms
  }
</script>
<template>
<modal-media>
  <template v-slot:header>
    Edit Game
  </template>
  <template v-slot="scope">
    <component :is="forms[formType ?? 'GameForms']" :game="game" :show-delete="true" @exit="scope.exit"/>
  </template>
</modal-media>
</template>
  