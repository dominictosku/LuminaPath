<script setup lang="ts">
const style = {
  '--swiper-navigation-color': '#fff',
  '--swiper-pagination-color': '#fff',
};
const speed = 600;
const pagination = {
  clickable: true,
}
const modules = [SwiperPagination, SwiperNavigation, SwiperParallax, SwiperAutoplay]
const autoplay = {
  delay: 4000,
  disableOnInteraction: false,
}

const containerStyle = {
  'background-image':
    'url(https://images.pexels.com/photos/956999/milky-way-starry-sky-night-sky-star-956999.jpeg)',
}
</script>
<template>
  <swiper :style="style" :speed="speed" :parallax="true" :pagination="pagination" :modules="modules" :autoplay="autoplay"
    class="mySwiper">
    <div slot="container-start" class="parallax-bg" :style="containerStyle" data-swiper-parallax="-23%"></div>
    <swiper-slide v-for="slide in 3">
      <news-activity />
    </swiper-slide>
  </swiper>
</template>
<style scoped>
.swiper {
  width: 100%;
  height: 100%;
  background: #000;
}

.parallax-bg {
  position: absolute;
  left: 0;
  top: 0;
  width: 130%;
  height: 100%;
  -webkit-background-size: cover;
  background-size: cover;
  background-position: center;
}
</style>