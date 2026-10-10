<script setup lang="ts">
// 安全地把 RichText（**粗體**、[文字](連結)）轉成 VNode；不使用 v-html。
import { NuxtLink } from '#components'
import { safeHref } from '#shared/utils/url'

const props = defineProps<{ text: string; tag?: string }>()

const TOKEN = /\*\*(.+?)\*\*|\[(.+?)\]\((\S+?)\)/g

function render() {
  const nodes: (string | VNode)[] = []
  let last = 0
  for (const m of props.text.matchAll(TOKEN)) {
    if (m.index > last) nodes.push(props.text.slice(last, m.index))
    if (m[1]) {
      nodes.push(h('strong', m[1]))
    } else {
      // 連結走白名單（javascript: 等不輸出成連結，只留文字）
      const href = safeHref(m[3]!)
      nodes.push(
        !href
          ? m[2]!
          : /^https:/i.test(href)
            ? h('a', { href, target: '_blank', rel: 'noopener noreferrer' }, m[2])
            : /^(tel|mailto):/i.test(href)
              ? h('a', { href }, m[2])
              : h(NuxtLink, { to: href }, () => m[2]),
      )
    }
    last = m.index + m[0].length
  }
  if (last < props.text.length) nodes.push(props.text.slice(last))
  return h(props.tag ?? 'span', nodes)
}
</script>

<template>
  <component :is="render" />
</template>
