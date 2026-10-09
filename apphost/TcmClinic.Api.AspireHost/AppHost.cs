using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// SQL Server 容器（開發時持久保留），並建立乾淨的資料庫「DefaultConnection」。
// 固定主機連接埠 1433，讓本機以 SSMS/Azure Data Studio 連 `localhost,1433` 穩定連線
// （密碼固定於 appsettings.Development.json 的 Parameters:sql-password；皆僅限開發）。
var sqlServer = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHostPort(1433);

var database = sqlServer.AddDatabase("DefaultConnection", "TcmClinicApi");

// 後端 API（資源名 "api"；.NET 專案為 TcmClinic.Api.Web）：注入連線、EF 於啟動時對乾淨庫套 migration。
builder.AddProject<Projects.TcmClinic_Api_Web>("api")
    .WithReference(database)
    .WaitFor(database)
    // 在 dashboard 的 api 資源多加一條指向 Scalar API 文件的連結（不改 app 根行為）。
    .WithUrls(context =>
        context.Urls.Add(new ResourceUrlAnnotation
        {
            Url = "/scalar",
            DisplayText = "Scalar",
            Endpoint = context.GetEndpoint("http"),
        }));

// 前端（frontend/）目前不在本編排內，於 frontend/ 以 pnpm dev:site、pnpm dev:admin 另行啟動，經 /api 代理到
// 上方 API 的 http 位址（docs/FRONTEND.md §5.1、§10）。若要一併啟動，把 API 資源存成變數，再以 AddExecutable
// 加入前端 dev server，並以 .WithReference(api) 注入後端位址。

builder.Build().Run();
