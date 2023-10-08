<script setup lang="ts">
import { square, triangle } from 'ionicons/icons';
</script>
<template>
    <IonPage>
        <IonContent :fullscreen="true">
            <NavigationNavBar />
            <ion-tabs>
                <ion-router-outlet class="mt-16"></ion-router-outlet>
                <ion-tab-bar slot="bottom">
                    <ion-tab-button tab="tab1" href="/Home">
                        <ion-icon aria-hidden="true" :icon="triangle" />
                        <ion-label>Home</ion-label>
                    </ion-tab-button>

                    <ion-tab-button tab="tab3" href="/MediaList/Games">
                        <ion-icon aria-hidden="true" :icon="square" />
                        <ion-label>MediaList</ion-label>
                    </ion-tab-button>

                    <ion-tab-button tab="tab4" href="/Planer">
                        <ion-icon aria-hidden="true" :icon="square" />
                        <ion-label>Planer</ion-label>
                    </ion-tab-button>

                    <ion-tab-button tab="tab5" href="/Statistic">
                        <ion-icon aria-hidden="true" :icon="square" />
                        <ion-label>Statistic</ion-label>
                    </ion-tab-button>
                </ion-tab-bar>
            </ion-tabs>
        </IonContent>
    </IonPage>
</template>

