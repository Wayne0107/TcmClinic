# language: zh-TW
# 來源：specs/其他/版本號與建置識別/spec.md（SPEC-065）
# 未納入：AC-3（組件資訊的解析規則，不經 API）由單元測試 BuildInfoTests 驗證（docs/TESTING.md §4.6）。
功能: 版本號與建置識別
  部署後，維運人員或部署腳本不需登入即可確認線上執行的版本號與 commit 是否與發版一致。

  背景:
    假設 我沒有登入

  @SPEC-065 @AC-1
  場景: 未登入即可查詢建置識別
    當 我查詢建置識別
    那麼 建置識別回應狀態為 200
    而且 建置識別回應恰好包含欄位 "version"、"sha"、"buildTime"

  @SPEC-065 @AC-2
  場景: 版本號與 sha 來自組件資訊
    當 我查詢建置識別
    那麼 回應的 version 為 SemVer 格式且等於組件版本
    而且 回應的 sha 為 40 碼小寫十六進位且等於組件寫入的 sha
