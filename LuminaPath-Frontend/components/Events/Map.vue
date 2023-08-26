<script setup>
const map = ref(null);
const zoom = ref(15);
const center = CalculateCenter();
const marker = ref(null);

const emits = defineEmits();

function CalculateCenter() {
    if (true) {
        return [70.15569, 30.31934]
    }
    return [latLng.lat, latLng.lng]
}

const CreateMarker = event => {
    const latLng = event.latlng;
    marker.value = { latLng };
    emits('addMarker', { latLng });
};

</script>
<template>
    <div style="height: 24vh; width: 43vh">
        <l-map ref="map" :zoom="zoom" :center="center" @click="CreateMarker">
            <l-tile-layer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution="&copy; <a href=&quot;https://www.openstreetmap.org/&quot;>OpenStreetMap</a> contributors"
                :layer-type="base" name="OpenStreetMap"></l-tile-layer>
            <l-marker v-if="marker !== null" :lat-lng="marker.latLng"></l-marker>
        </l-map>
    </div>
</template>

<style scoped>
</style>