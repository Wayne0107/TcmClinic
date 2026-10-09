# 非功能需求：無障礙（a11y）

## 1. 已做到 [觀察]

- `<html lang="zh-Hant-HK">`。
- 每頁單一 H1，標題階層大致正確。
- 主導覽 `<nav aria-label="主要導覽">`、麵包屑 `aria-label="麵包屑"`、分頁 `aria-label="分頁"`。
- 漢堡按鈕為 `<button>`，有 `aria-label="開啟選單"` 與 `aria-expanded` 同步。
- 目前頁連結 `aria-current="page"`。
- 輪播每張 `role="group"` + `aria-label`；圓點按鈕 `aria-label="第N張"`。
- 表單每個欄位都有 `<label for>`；必填欄位標 `*` 並設 `required`；狀態訊息 `role="status"`。
- 蜜罐欄位 `aria-hidden="true"`、`tabindex="-1"`。
- FAQ 使用原生 `<details>`／`<summary>`，鍵盤可操作。
- 燈箱支援 Esc 與方向鍵。
- 圖片有中文 alt。

## 2. 問題

1. **未處理 `prefers-reduced-motion`**：輪播自動播放、Reveal 動畫、WhatsApp 浮動按鈕持續跳動都無法關閉。
2. **輪播無暫停鈕**：自動每 5 秒切換，不符合 WCAG 2.2.2（暫停、停止、隱藏）。
3. **Reveal 預設隱藏**：內容初始 `opacity:0`，JS 失敗時依賴 fallback；IntersectionObserver 不存在才顯示。
4. **統計數字**：HTML 初始為「0」，螢幕閱讀器可能讀到 0。
5. **燈箱**：未見焦點鎖定（focus trap）、開啟時焦點未移入、關閉後未還原焦點 [觀察 site.js]。
6. **桌面下拉選單**：只用 `:hover` 顯示，鍵盤 Tab 進入「診療服務」時子選單不會展開。
7. **Emoji 圖示**：📍📞🕒💬 等作為資訊前綴，螢幕閱讀器會讀出 emoji 名稱。
8. **聯絡頁相片 alt 重複**：多張相片同為「診所門面招牌」或「診所環境」。
9. **影片**：無字幕或文字說明。

## 3. 新版目標

符合 WCAG 2.1 AA，至少修正上述 1、2、5、6。
