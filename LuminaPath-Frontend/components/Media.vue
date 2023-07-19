<script setup lang="ts">
import LuminaGrid from './Media/LuminaGrid.vue';
import Details from '@/components/Media/Details.vue';

const isGrid = ref(false)

defineProps({
  name: String,
});

function changeIsGrid() {
  isGrid.value = !isGrid.value
}
</script>

<template>
  <ion-nav-link ref="detailsEvent" router-direction="forward" :component="Details">
  <IonButton> click here </IonButton>
  </ion-nav-link>
  <MediaTabs @changebool="changeIsGrid" />
  <MediaLuminaFilter />
    <!-- Table view -->
    <div v-if="!isGrid" id="Table" class="tabcontent">
      <MediaLuminaTable />
    </div>
    <!-- Gallery view -->
    <div v-else id="Grid" class="tabcontent">
      <LuminaGrid />
    </div>
  <MediaLuminaPagination />
</template>
  
<style scoped>
/* Style the tab content */
.tabcontent {
  padding: 6px 12px;
  -webkit-animation: fadeEffect 1s;
  animation: fadeEffect 1s;
}

/* Fade in tabs */
@-webkit-keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}
</style>
  