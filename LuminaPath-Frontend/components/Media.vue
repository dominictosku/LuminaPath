<script setup lang="ts">
const isGrid = ref(false)
defineProps({
  name: String,
});

function changeIsGrid() {
  isGrid.value = !isGrid.value
}
</script>

<template>
  <MediaTabs @changebool="changeIsGrid" />
    <!-- Table view -->
    <div v-if="!isGrid" id="Table" class="tabcontent">
      <MediaTable />
    </div>
    <!-- Gallery view -->
    <div v-else id="Grid" class="tabcontent">
      <MediaGrid />
    </div>
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
  