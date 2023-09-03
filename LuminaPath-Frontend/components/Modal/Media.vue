<script setup lang="ts">
const modal: any = ref(null)

function cancel() {
  modal.value.$el.dismiss(null, 'cancel');
}
</script>
<template>
  <ion-content class="ion-padding">
    <ion-button id="open-modal" expand="block"><slot name="header"></slot></ion-button>
    <ion-modal ref="modal" trigger="open-modal">
      <ion-header>
        <ion-toolbar>
          <ion-button @click="cancel()" slot="start">
            X
          </ion-button>
          <ion-title> 
            <slot name="header"></slot>
          </ion-title>
        </ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">
        <slot :exit="cancel"></slot>
      </ion-content>
    </ion-modal>
  </ion-content>
</template>
<style scoped>
  ion-toast.custom-toast {
    --background: red;
    --box-shadow: 3px 3px 10px 0 rgba(0, 0, 0, 0.2);
    --color: #4b4a50;
  }
</style>
  