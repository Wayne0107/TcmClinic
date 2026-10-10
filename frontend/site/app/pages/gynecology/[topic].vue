<script setup lang="ts">
// gynecology 主題子頁（如 /gynecology/xxx）；找不到主題時由 ServiceTopicView 回 404
const route = useRoute()
</script>

<template>
  <ServiceTopicView service="gynecology" :topic="route.params.topic as string" />
</template>
