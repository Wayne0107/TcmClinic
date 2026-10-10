<script setup lang="ts">
// beauty 主題子頁（如 /beauty/xxx）；找不到主題時由 ServiceTopicView 回 404
const route = useRoute()
</script>

<template>
  <ServiceTopicView service="beauty" :topic="route.params.topic as string" />
</template>
