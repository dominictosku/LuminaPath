<script setup lang="ts">
const style = {
  '--swiper-navigation-color': '#fff',
  '--swiper-pagination-color': '#fff',
};
const pagination = {
  clickable: true,
}
const modules = [SwiperPagination, SwiperNavigation]
</script>
<template>
  <swiper :style="style" :pagination="pagination" :modules="modules"
    class="mySwiper">
    <div slot="container-start"></div>
    <swiper-slide v-for="slide in 4">
        <div class="grid grid-cols-1 md:grid-cols-4">
            <news-media v-for="item in 4" />
        </div>
    </swiper-slide>
  </swiper>
</template>
<style scoped>
</style>