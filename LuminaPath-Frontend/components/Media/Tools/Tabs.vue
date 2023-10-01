<script setup lang="ts">

function SwitchView(evt: any) {
    const tablinks: any = document.getElementsByClassName("tablinks");

	for (let i = 0; i < tablinks.length; i++) {
		tablinks[i].className = tablinks[i].className.replace(" active", "");
	}
	evt.currentTarget.className += " active";
}
</script>

<template>
<div class="flex">
	<div class="tab">
		<button id="defaultOpen" class="tablinks active" @click="SwitchView($event),$emit('changebool')">Table</button>
		<button class="tablinks" @click="SwitchView($event),$emit('changebool')">Grid</button>
	</div>
</div>
</template>

<style scoped>
/* Style the tab */
.tab {
    overflow: hidden;
}

    /* Style the buttons inside the tab */
    .tab button {
        background-color: inherit;
        margin-left: 1em;
        border: none;
        outline: none;
        cursor: pointer;
        padding: 0.5em 0.5em;
        transition: 0.3s;
        font-size: 1rem;
    }

        /* Change background color of buttons on hover */
        .tab button:hover {
            color:black;
            background-color: white;
        }

        /* Create an active/current tablink class */
        .tab button.active {
            color:white;
            background-color: cornflowerblue;
        }
</style>