<script setup lang="ts">
import Modal from '~/components/Forms/Create.vue';
import { create } from 'ionicons/icons';
const props = defineProps({
  gameId: Number
})
const modalProps = {formType: "MyGameForms", gameId: props.gameId}
</script>
<template>
    <div>
        <ion-fab-button class="absolute right-2 top-0" @click="openModal(Modal, modalProps)" size="small">
            <ion-icon :icon="create"></ion-icon>
        </ion-fab-button>
    </div>
</template>
<style scoped></style>