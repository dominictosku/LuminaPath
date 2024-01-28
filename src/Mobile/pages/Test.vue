<script setup>
import { ref } from 'vue'
const complete = ref(false)

const submitHandler = async (data) => {
  // We need to submit this as a multipart/form-data
  // to do this we use the FormData API.
  const body = new FormData()
  // Finally, we append the actual File object(s)
  body.append('file', data.image)

  console.log(data)

  const response = await $fetch('https://localhost:16051/api/games/image', {
    method: 'POST',
    headers: {
      'Content-Type': 'multipart/form-data',
    },
    body: body,
  });
}
</script>

<template>
  <FormKit id="imgForm" type="form" @submit="submitHandler">
    <FormKit type="file" label="image" name="image" accept=".jpg,.png,.pdf" validation="required" />
  </FormKit>
</template>