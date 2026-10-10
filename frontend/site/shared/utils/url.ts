// 網址安全工具：前台元件與 Nitro middleware 共用。

/**
 * 結尾斜線 301 的目標（/about/ → /about）；不需導向時回 null。
 * 開頭的連續斜線、反斜線收成一個 /，避免導向 //evil.com 這類協定相對網址（開放重新導向）。
 */
export function trailingSlashRedirect(pathname: string, search: string): string | null {
  if (pathname.length <= 1 || !pathname.endsWith('/') || pathname.startsWith('/api/')) return null
  const target = pathname.replace(/\/+$/, '').replace(/^[/\\]+/, '/')
  return (target || '/') + search
}

// 允許的連結：站內路徑（單一 / 開頭）、錨點、https、tel、mailto
const SAFE_HREF = /^(\/(?![/\\])|#|https:\/\/|tel:|mailto:)/i

/** 內容連結白名單；不在白名單內（javascript:、data:、//外站、http 明碼等）回 null，呼叫端改輸出純文字 */
export function safeHref(href: string): string | null {
  return SAFE_HREF.test(href.trim()) ? href : null
}
