<script setup lang="ts">
import { Game, Plattforms } from "@/utils/models/games"
import { useGameStore } from "@/stores/games"
const store = useGameStore()

let newGame = new Game(0, "", "", "", 0, 0)
const game: Ref<Game> = ref(newGame)

const isModalOpen = ref(false)

async function post() {
    await store.createGame(game.value)
    closeModal()
}

function openModal() {
    isModalOpen.value = true;
}
function closeModal() {
    isModalOpen.value = false;
}

defineExpose({
    openModal,
    closeModal
})

</script>  
<template>
    <div>
        <!-- Modal -->
        <transition name="modal">
            <div v-if="isModalOpen" class="absolute inset-0 flex items-center justify-center z-50" id="exampleModal"
                tabindex="-1" aria-labelledby="exampleModalLabel" aria-modal="true">
                <div class="modal-overlay" @click="closeModal"></div> <!-- Transparent background overlay -->
                <div class="max-w-md mx-auto rounded shadow-lg">
                    <!-- Modal content -->
                    <form class="grid justify-center mt-12">
                        <div class="border-solid border-2 border-sky-500 p-6 bg-gray-800">
                            <FormKit v-model="game.name" name="Title" label="Title of game" validation="required" />
                            <FormKit v-model="game.description" type="textarea" name="description" label="description" />
                            <FormKit v-model="game.plattforms" type="select" name="plattform" label="Plattform"
                                placeholder="Playstation" :options="Plattforms" />
                            <FormKit v-model="game.genre" type="text" name="genre" label="genre" />
                            <FormKit v-model="game.playtime" type="number" label="Estimated Playtime" step="1" />
                            <div class="flex justify-end p-3 gap-3">
                                <IonButton color="light" @click="closeModal">close</IonButton>
                                <IonButton @click="post"> Add </IonButton>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </transition>

        <!-- Modal Backdrop -->
        <div v-if="isModalOpen" class="fixed inset-0 bg-black opacity-50" id="backdrop"></div>
    </div>
</template>
<style scoped>
.modal-enter-active {
    animation: modalEnter 0.3s ease-out;
}

.modal-leave-active {
    animation: modalLeave 0.3s ease-in;
}

@keyframes modalEnter {
    0% {
        transform: translateY(-100%);
        opacity: 0;
    }

    100% {
        transform: translateY(0);
        opacity: 1;
    }
}

@keyframes modalLeave {
    0% {
        transform: translateY(0);
        opacity: 1;
    }

    100% {
        transform: translateY(-100%);
        opacity: 0;
    }
}

.modal-overlay {
    position: fixed;
    inset: 0;
    z-index: -1;
}
</style>
  