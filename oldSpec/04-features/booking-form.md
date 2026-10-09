# 功能規格：網上預約表單

## 1. 概述

訪客填寫預約**請求**，診所人員之後以電話或 WhatsApp 聯絡確認。提交**不代表**預約已確定。系統不做即時排期或時段容量檢查 [推論]。

| 項目 | 內容 |
|------|------|
| 出現位置 | 首頁 `/#booking`（`id="home-booking-form"`）、聯絡頁 `/contact#booking`（`id="contact-booking-form"`） |
| 送出方式 | `POST /appointment/submit`，以 AJAX（fetch）送出 `FormData`（multipart/form-data） |
| 前端程式 | `site.js` 中 `.js-booking-form` 的 submit 事件 |

## 2. 欄位 [觀察]

| 欄位 name | 標籤 | 類型 | 必填 | 限制／選項 | placeholder |
|-----------|------|------|------|-----------|-------------|
| `_token` | — | hidden | ✓ | CSRF token（每個 session 產生 64 字元 hex） | |
| `source_page` | — | hidden | ✓ | `home` 或 `contact` | |
| `website` | 網站 | text（蜜罐） | ✗ | 畫面外隱藏（`.hp-field`，`aria-hidden`、`tabindex=-1`、`autocomplete=off`）；真人不會填寫 | |
| `name` | 姓名 * | text | ✓ | `maxlength=80`，`autocomplete=name` | 您的姓名 |
| `phone` | 聯絡電話 * | tel | ✓ | `maxlength=24`，`autocomplete=tel` | 聯絡電話 |
| `branch_id` | 選擇分店 * | select | ✓ | `1`=觀塘裕民中心、`2`=藍田匯景商場 | 請選擇 |
| `appointment_date` | 預約日期 * | date | ✓ | `min` = 伺服器當日日期（例 `2026-10-09`）；無 max，未阻擋星期日 | |
| `requested_slot` | 預約時段 * | select | ✓ | `morning`=上午 (10:30–13:30)、`afternoon`=下午 (14:30–19:30) | 請選擇 |
| `service_id` | 診療項目 * | select | ✓ | 見下表 | 請選擇 |
| `notes` | 備註／症狀描述 | textarea（3 行） | ✗ | `maxlength=2000` | 簡述您的不適或需要（可選） |
| `consent` | 我同意診所使用以上資料處理及聯絡此預約。* | checkbox | ✓ | value=`1` | |

### 2.1 診療項目選項（`service_id`）

| 值 | 顯示文字 |
|----|---------|
| 1 | 中醫婦科 |
| 2 | 婦女不孕不育調理 |
| 3 | 懷孕安胎 |
| 4 | 產後調理／產後扎肚 |
| 5 | 面部經絡理療／中藥面膜 |
| 6 | 美容針 |
| 7 | 減肥修身／針灸減肥 |
| 8 | 腹部暖宮 |
| 9 | 中醫全科診症 |

### 2.2 版面

- 欄位兩兩一列（`.two`）：姓名＋電話／分店＋日期／時段＋項目；≤560px 改單欄。
- 下方：備註（全寬）、同意勾選、提交按鈕「確認提交預約」、提示「送出後診所會以電話或 WhatsApp 聯絡確認，提交並不代表預約已確定。」
- 表單上方有狀態訊息區 `<div class="js-booking-status" role="status">`（空時隱藏）。
- 首頁表單左側為「診所資訊」卡：兩分店、電話、應診時間、WhatsApp 預約按鈕。

## 3. 前端流程 [觀察]

```
使用者按「確認提交預約」
 → 瀏覽器原生 HTML5 驗證（required、maxlength、type、min）
 → preventDefault，按鈕 disabled，文字改為「提交中…」，清空狀態訊息
 → fetch(POST form.action, headers: Accept: application/json, X-Requested-With: XMLHttpRequest, body: FormData)
 → 解析 JSON 回應 { ok: boolean, message: string }
     ok = true  → 狀態區顯示 message（綠色 is-success），form.reset()，狀態區平滑捲至畫面中央
     ok = false → 狀態區顯示 message（紅色 is-error）
     無 message → 顯示「系統暫時未能處理要求。」
 → 網絡錯誤／JSON 解析失敗 → 顯示「網絡連線失敗，請使用 WhatsApp 或電話聯絡我們。」（is-error）
 → finally：按鈕恢復可用，文字還原
```

## 4. 後端行為 [推論]

> 為避免對正式環境寫入假預約，未實際送出測試。以下依前端與欄位推論，新版需與業主確認。

| 項目 | 推論行為 |
|------|---------|
| CSRF | 驗證 `_token` 與 session（cookie `SYTTCM_ADMIN`）相符 |
| 防垃圾 | `website` 有值即視為機器人，靜默拒絕或回傳成功 |
| 驗證 | 必填、長度、分店／時段／服務 ID 合法、日期 ≥ 今日 |
| 儲存 | 寫入預約紀錄（含 `source_page`）供後台 `/admin` 檢視 |
| 通知 | 可能通知診所（Email／後台）；未見向使用者發送確認訊息的跡象 |
| 回應 | JSON `{ "ok": true/false, "message": "..." }` |
| 非 AJAX 送出 | 未知（JS 停用時表單會以一般 POST 送出） |

## 5. 已知限制

- 日期選擇未排除星期日（休診日）及公眾假期。
- 未依分店、醫師區分可預約時段。
- 電話欄位無格式驗證（僅長度）。
- 無 reCAPTCHA 等額外防護，只靠蜜罐 + CSRF。
- 送出後無確認編號，亦無 Email／SMS 自動回覆。

## 6. 驗收要點（新版應至少達到）
- [ ] 所有必填欄位缺漏時無法送出，並提示錯誤。
- [ ] 蜜罐欄位對真人不可見、不可聚焦。
- [ ] 成功送出顯示確認訊息並清空表單；失敗保留已填資料。
- [ ] 網絡失敗時提示改用 WhatsApp／電話。
- [ ] 送出期間按鈕不可重複點擊。
- [ ] 兩處表單使用相同 API，以 `source_page` 區分來源。
