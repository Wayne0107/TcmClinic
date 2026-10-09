# 第三方相依與授權（THIRD-PARTY-NOTICES）

依憲章 Principle XVI 維護。每次新增／移除／升級相依時，必須在同一次變更更新本檔。
所有相依皆為寬鬆授權（MIT / Apache-2.0 / BSD-2-Clause）；copyleft 相依需維護者核准。

Manifest 對照：
- 後端 NuGet 版本：根目錄 `Directory.Packages.props`（Central Package Management）
- 後端工具：`.config/dotnet-tools.json`

## Production（隨部署產物出貨）

### 後端（.NET）

| 套件 | Manifest | 用途 | 授權 | 來源 |
|------|----------|------|------|------|
| Ardalis.GuardClauses | Directory.Packages.props | 邊界 fail-fast 驗證 | MIT | https://github.com/ardalis/GuardClauses |
| Ardalis.Result | Directory.Packages.props | Result 模式錯誤處理 | MIT | https://github.com/ardalis/Result |
| Ardalis.SmartEnum | Directory.Packages.props | 智慧列舉 | MIT | https://github.com/ardalis/SmartEnum |
| Ardalis.Specification (+ .EntityFrameworkCore) | Directory.Packages.props | 查詢規格 | MIT | https://github.com/ardalis/Specification |
| Vogen | Directory.Packages.props | 強型別 ID（source gen） | Apache-2.0 | https://github.com/SteveDunn/Vogen |
| Mediator.Abstractions / Mediator.SourceGenerator | Directory.Packages.props | CQRS 中介（source gen） | MIT | https://github.com/martinothamar/Mediator |
| Microsoft.EntityFrameworkCore.SqlServer / Design | Directory.Packages.props | ORM / 資料存取 | MIT | https://github.com/dotnet/efcore |
| FastEndpoints / FastEndpoints.Swagger | Directory.Packages.props | REPR API 框架 / OpenAPI | MIT | https://github.com/FastEndpoints/FastEndpoints |
| Microsoft.AspNetCore.Authentication.JwtBearer | Directory.Packages.props | 登入 JWT Bearer 驗證（SPEC-004 FR-021，ADR-011） | MIT | https://github.com/dotnet/aspnetcore |
| Scalar.AspNetCore | Directory.Packages.props | OpenAPI 互動式文件 UI（/scalar） | MIT | https://github.com/scalar/scalar |
| Serilog.AspNetCore | Directory.Packages.props | 結構化日誌 | Apache-2.0 | https://github.com/serilog/serilog-aspnetcore |
| Serilog.Sinks.Console | Directory.Packages.props | Console 日誌輸出 | Apache-2.0 | https://github.com/serilog/serilog-sinks-console |
| Serilog.Sinks.File | Directory.Packages.props | 正式/IIS 持久化 rolling 檔案日誌（Principle XVII） | Apache-2.0 | https://github.com/serilog/serilog-sinks-file |
| Serilog.Settings.Configuration | Directory.Packages.props | 由 appsettings 設定 sink/等級（ReadFrom.Configuration，Principle XVII） | Apache-2.0 | https://github.com/serilog/serilog-settings-configuration |
| Serilog.Expressions | Directory.Packages.props | 生產 log 檔的 JSON 格式（ExpressionTemplate）與等級篩選（ADR-008） | Apache-2.0 | https://github.com/serilog/serilog-expressions |
| Aspire.Hosting.SqlServer | Directory.Packages.props | Aspire SQL Server 編排 | MIT | https://github.com/dotnet/aspire |
| TreeCollections | Directory.Packages.props | 階層／選單樹建模 | MIT | https://github.com/davidwest/TreeCollections |
| Microsoft.Extensions.Http.Resilience | Directory.Packages.props | HTTP 韌性 | MIT | https://github.com/dotnet/extensions |
| Microsoft.Extensions.ServiceDiscovery | Directory.Packages.props | 服務發現（Aspire） | MIT | https://github.com/dotnet/aspire |
| Microsoft.Extensions.Logging.Abstractions | Directory.Packages.props | 日誌抽象 | MIT | https://github.com/dotnet/runtime |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | Directory.Packages.props | OTLP 匯出 | Apache-2.0 | https://github.com/open-telemetry/opentelemetry-dotnet |
| OpenTelemetry.Extensions.Hosting | Directory.Packages.props | OTel 主機整合 | Apache-2.0 | https://github.com/open-telemetry/opentelemetry-dotnet |
| OpenTelemetry.Instrumentation.AspNetCore / Http / Runtime | Directory.Packages.props | OTel 儀器 | Apache-2.0 | https://github.com/open-telemetry/opentelemetry-dotnet-contrib |

## Dev / Test / Build-only（不隨產物出貨）

### 後端（.NET）

| 套件 | Manifest | 用途 | 授權 | 來源 |
|------|----------|------|------|------|
| xunit.v3 / xunit.runner.visualstudio | Directory.Packages.props | 測試框架 / 執行器 | Apache-2.0 | https://github.com/xunit/xunit |
| Microsoft.NET.Test.Sdk | Directory.Packages.props | 測試 SDK | MIT | https://github.com/microsoft/vstest |
| NSubstitute | Directory.Packages.props | Mock | BSD-2-Clause | https://github.com/nsubstitute/NSubstitute |
| Microsoft.AspNetCore.Mvc.Testing | Directory.Packages.props | WebApplicationFactory | MIT | https://github.com/dotnet/aspnetcore |
| Testcontainers.MsSql | Directory.Packages.props | 功能測試 SQL 容器 | MIT | https://github.com/testcontainers/testcontainers-dotnet |
| coverlet.collector | Directory.Packages.props | 覆蓋率 | MIT | https://github.com/coverlet-coverage/coverlet |
| CsCheck | Directory.Packages.props | property-based 測試 | Apache-2.0 | https://github.com/AnthonyLloyd/CsCheck |
| Verify.XunitV3 | Directory.Packages.props | 契約 snapshot 測試 | MIT | https://github.com/VerifyTests/Verify |
| NetArchTest.Rules | Directory.Packages.props | 架構測試 | MIT | https://github.com/BenMorris/NetArchTest |
| Reqnroll.xunit.v3 | Directory.Packages.props | BDD 驗收情境（Gherkin `.feature` → xUnit v3 測試；2026-10-06 引進） | BSD-3-Clause | https://github.com/reqnroll/Reqnroll |
| dotnet-stryker | .config/dotnet-tools.json | 變異測試（工具） | Apache-2.0 | https://github.com/stryker-mutator/stryker-net |
| coverlet.console | .config/dotnet-tools.json | 覆蓋率量測（工具；測試以 `dotnet run` 執行，collector 無法使用） | MIT | https://github.com/coverlet-coverage/coverlet |
| gitleaks | 開發機安裝（winget），設定檔 `.gitleaks.toml` | commit 前秘密掃描（工具，pre-commit hook） | MIT | https://github.com/gitleaks/gitleaks |
| actions/checkout | .github/workflows/ci.yml | CI 取出原始碼 | MIT | https://github.com/actions/checkout |
| actions/setup-dotnet | .github/workflows/ci.yml | CI 安裝 .NET SDK | MIT | https://github.com/actions/setup-dotnet |

## 授權合規備註

- 全部為寬鬆授權，可商用、可閉源散布；散布時須保留各套件的版權與授權聲明（MIT/BSD）
  或依 Apache-2.0 第 4 條附上 `NOTICE`（如有）。
- 目前無 copyleft（GPL/LGPL/AGPL/MPL/SSPL）相依。
- 授權以查證日 2026-08-26 各專案 GitHub 為準；升級大版本時應重新確認。
- **Verify／Verify.XunitV3 固定在 31.x，不得升到 33 以上**：33.0.0 起授權由 MIT 改為自訂的 `OsmfEula`，
  商業使用須付維護費（建置時以 SponsorCheck SC021 擋下；免除條件僅限開源無營收或年營收低於 1 萬美元）。
  依 `docs/DEPENDENCIES.md` §2 屬須審查核准的自訂授權。32.0.1 為最後的 MIT 版本，但無後續更新，
  故維持 31.28.0。查證日 2026-10-05（nuget.org 各版 nuspec 的 `license` 欄位）。
