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

const router = useIonRouter()
</script>
<template>
  <swiper :style="style" :speed="speed" :parallax="true" :pagination="pagination" :modules="modules" :autoplay="autoplay"
    class="mySwiper">
    <div slot="container-start" class="parallax-bg" :style="containerStyle" data-swiper-parallax="-23%"></div>
    <swiper-slide v-for="slide in 3">
      <div class="flex flex-col items-center NewsCard">
        <img class="object-cover w-full rounded-t-lg h-96 md:h-auto md:w-48 md:rounded-none md:rounded-l-lg"
          src="https://images.pexels.com/photos/956999/milky-way-starry-sky-night-sky-star-956999.jpeg" alt="">
        <div class="flex flex-col justify-between p-4 leading-normal">
          <h5 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
            New Games this month
          </h5>
          <p class="mb-3 font-normal text-gray-700 dark:text-gray-400">
            Track your games and share progress with friends
          </p>
          <ion-button @click="router.push('/MediaList/Games')">Games</ion-button>
        </div>
      </div>
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

.NewsCard {
  @apply bg-white border border-gray-200 rounded-lg shadow md:flex-row md:max-w-xl hover:bg-gray-100 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700
}
</style>