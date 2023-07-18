<script setup lang="ts">
import draggable from "vuedraggable";
let idGlobal = 8;
const planned = ref([
    { name: "Washing", id: 1 },
    { name: "Cleaning", id: 2 },
    { name: "Eating", id: 3 }
])

const inProgress = ref([
    { name: "Sleeping", id: 5 },
    { name: "Gaming", id: 6 }
])

const controlOnStart = ref(true)

function clone({ name }: any) {
    return { name, id: idGlobal++ };
}
function pullFunction() {
    return controlOnStart.value ? "clone" : true;
}
function start({ originalEvent }: any) {
    controlOnStart.value = originalEvent.ctrlKey;
}
</script>
<style scoped></style>
<template>
    <div class="flex justify-evenly gap-4">
        <div class="col-auto">
            <h3>Planned</h3>
            <draggable class="dragArea list-group" :list="planned" :clone="clone"
                :group="{ name: 'todo', pull: pullFunction }" @start="start" item-key="id">
                <template #item="{ element }">
                    <div class="list-group-item">
                        {{ element.name }}
                    </div>
                </template>
            </draggable>
        </div>

        <div class="col-auto">
            <h3>Progressing</h3>
            <draggable class="dragArea list-group" :list="inProgress" group="todo" item-key="id">
                <template #item="{ element }">
                    <div class="list-group-item">
                        {{ element.name }}
                    </div>
                </template>
            </draggable>
        </div>
    </div>
</template>
  