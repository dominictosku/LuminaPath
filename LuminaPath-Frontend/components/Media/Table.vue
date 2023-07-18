<script setup lang="ts">
</script>
<template>
    <section class="container px-4 mx-auto">
        <MediaLuminaFilter />
        <div class="flex flex-col mt-6">
            <div class="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
                <div class="inline-block min-w-full py-2 align-middle md:px-6 lg:px-8">
                    <div class="overflow-hidden border border-gray-200 dark:border-gray-700 md:rounded-lg">
                        <MediaLuminaTable />
                    </div>
                </div>
            </div>
        </div>
        <MediaLuminaPagination />
    </section>
</template>