<script setup lang="ts">
import Modal from '~/components/Forms/Create.vue';
import { create } from 'ionicons/icons';
const props = defineProps({
  gameId: Number
})
const modalProps = {formType: "MyGames", gameId: props.gameId}
</script>
<template>
    <div>
        <ion-fab class="absolute right-2 top-0">
            <ion-fab-button class="" @click="openModal(Modal, modalProps)" size="small">
                <ion-icon :icon="create"></ion-icon>
            </ion-fab-button>
        </ion-fab>
    </div>
</template>
<style scoped></style>