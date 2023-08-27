<script setup lang="ts">
import { add } from 'ionicons/icons';
import { modalController } from '@ionic/vue';
import Modal from '~/components/Forms/Create.vue';

  const message = ref('This modal example uses the modalController to present and dismiss modals.');

  const openModal = async () => {
    const modal = await modalController.create({
      component: Modal,
    });

    modal.present();

    const { data, role } = await modal.onWillDismiss();

    if (role === 'confirm') {
      message.value = `Hello, ${data}!`;
    }
  };
</script>
<template>
    <div class="sm:flex sm:items-center sm:justify-between">
        <div>
            <div class="flex items-center gap-x-3">
                <h2 class="text-lg font-medium text-gray-800 dark:text-white">Games</h2>

                <span class="px-3 py-1 text-xs text-blue-600 bg-blue-100 rounded-full
                            dark:bg-gray-800 dark:text-blue-400">240 Games</span>
            </div>

            <p class="mt-1 text-sm text-gray-500 dark:text-gray-300">These are our listed games</p>
        </div>

        <div class="flex items-center mt-4 gap-x-3">
            <button class="media-button bg-white border media-button dark:hover:bg-gray-800 dark:bg-gray-900
            hover:bg-gray-100 dark:text-gray-200 dark:border-gray-700 text-gray-700">
            <nuxt-icon name="cloud-download" filled />
            <span>Import</span>
        </button>
        <div class="media-button">
            <ion-fab class="z-0">
                <ion-fab-button @click="openModal" size="small">
                    <ion-icon :icon="add"></ion-icon>
                </ion-fab-button>
            </ion-fab>
        </div>
        </div>
    </div>

    <div class="mt-6 md:flex md:items-center md:justify-between">
        <div class="inline-flex flex-wrap overflow-hidden bg-white border divide-x
                    rounded-lg dark:bg-gray-900 rtl:flex-row-reverse dark:border-gray-700
                    dark:divide-gray-700">
            <button class="px-5 py-2 text-xs font-medium text-gray-600
                            transition-colors duration-200 bg-gray-100 sm:text-sm dark:bg-gray-800
                            dark:text-gray-300">
                View all
            </button>

            <button class="px-5 py-2 text-xs font-medium text-gray-600
                            transition-colors duration-200 sm:text-sm dark:hover:bg-gray-800
                            dark:text-gray-300 hover:bg-gray-100">
                Completed
            </button>

            <button class="px-5 py-2 text-xs font-medium text-gray-600 transition-colors
                            duration-200 sm:text-sm dark:hover:bg-gray-800 dark:text-gray-300
                            hover:bg-gray-100">
                Progressing
            </button>
        </div>

        <div class="relative flex items-center mt-4 md:mt-0">
            <span class="absolute">
                <nuxt-icon name="search" filled />
            </span>

            <input type="text" placeholder="Search" class="block w-full py-1.5 pr-5 text-gray-700
             bg-white border border-gray-200 rounded-lg md:w-80 placeholder-gray-400/70 pl-11
              rtl:pr-11 rtl:pl-5
             dark:bg-gray-900 dark:text-gray-300 dark:border-gray-600
              focus:border-blue-400 dark:focus:border-blue-300 focus:ring-blue-300
               focus:outline-none focus:ring focus:ring-opacity-40">
        </div>
    </div>
</template>