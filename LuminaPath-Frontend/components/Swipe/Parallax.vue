<script setup lang="ts">
</script>
<template>
  <swiper :style="{
    '--swiper-navigation-color': '#fff',
    '--swiper-pagination-color': '#fff',
  }" :speed="600" :parallax="true" :pagination="{
  clickable: true,
}" :modules="[SwiperPagination, SwiperNavigation, SwiperParallax, SwiperAutoplay]" :autoplay="{
  delay: 4000,
  disableOnInteraction: false,
}" class="mySwiper">
    <div slot="container-start" class="parallax-bg" :style="{
      'background-image':
        'url(https://media.istockphoto.com/id/1494263866/de/foto/abstrakte-verbundene-punkte-und-linien-konzept-der-ki-technologie-bewegung-des-digitalen.webp?s=1024x1024&w=is&k=20&c=JSgYVdISUmHqzOQ_2HwLZZCaFvx2oOcAU9QAbIWMn4U=)',
    }" data-swiper-parallax="-23%"></div>
    <swiper-slide v-for="slide in 5">
        <div class="grid max-w-screen-xl px-4 py-8 mx-auto lg:gap-8 xl:gap-0 lg:py-16 lg:grid-cols-12">
          <div class="mr-auto place-self-center lg:col-span-7">
            <h1
              class="max-w-2xl mb-4 text-4xl font-extrabold tracking-tight leading-none md:text-5xl xl:text-6xl dark:text-white">
              Welcome To Harare City</h1>
            <p class="max-w-2xl mb-6 font-light text-gray-500 lg:mb-8 md:text-lg lg:text-xl dark:text-gray-400">Welcome to
              [Company Name], your gateway to unforgettable travel experiences. We are a leading travel and tour company
              dedicated to creating exceptional journeys for adventurers, explorers, and wanderers like you.</p>
            <a href="#"
              class="inline-flex items-center justify-center px-5 py-3 mr-3 text-base font-medium text-center text-white rounded-lg bg-primary-700 hover:bg-primary-800 focus:ring-4 focus:ring-primary-300 dark:focus:ring-primary-900">
              Get started
              <svg class="w-5 h-5 ml-2 -mr-1" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
                <path fill-rule="evenodd"
                  d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z"
                  clip-rule="evenodd"></path>
              </svg>
            </a>
            <a href="#"
              class="inline-flex items-center justify-center px-5 py-3 text-base font-medium text-center text-gray-900 border border-gray-300 rounded-lg hover:bg-gray-100 focus:ring-4 focus:ring-gray-100 dark:text-white dark:border-gray-700 dark:hover:bg-gray-700 dark:focus:ring-gray-800">
              Speak to Sales
            </a>
          </div>
          <div class="hidden lg:mt-0 lg:col-span-5 lg:flex rounded-lg">
            <img src="https://lp-cms-production.imgix.net/2019-06/554369495_full.jpg" alt="mockup" class="rounded-lg">
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
}</style>