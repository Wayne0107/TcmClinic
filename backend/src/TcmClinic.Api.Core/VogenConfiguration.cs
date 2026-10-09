using Vogen;

// 全域 Vogen 預設：僅 System.Text.Json 轉換。
// EF Core value converter 不在此產生（會使 Core 依賴 EF，違反 Principle I）；
// EF 轉換改由 Infrastructure 的 IEntityTypeConfiguration 以 HasConversion 明確設定。
[assembly: VogenDefaults(conversions: Conversions.SystemTextJson)]
