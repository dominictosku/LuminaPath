<script setup lang="ts">
let newGame = new Game(0, "", "", "", 0, 0)

const isModalOpen = ref(false)

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
            <div v-if="isModalOpen" class="absolute inset-0 items-center justify-center z-50" id="exampleModal"
                tabindex="-1" aria-labelledby="CreateModal" aria-modal="true">
                <div class="modal-overlay" @click="closeModal"></div> <!-- Transparent background overlay -->
                <div class="w-fit m-auto rounded shadow-lg bg-slate-900 p-6">
                    <!-- Modal content -->
                    <FormsGameForm :game="newGame" @exit="closeModal" />
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
  