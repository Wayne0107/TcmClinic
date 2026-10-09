using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TcmClinic.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysAccessLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, comment: "主鍵：訪問紀錄流水號（資料庫產生）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "訪問類型：Read=查詢／Export=匯出／Download=下載"),
                    EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "被訪問的實體型別名（如 User）"),
                    PrimaryKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true, comment: "被訪問資料的主鍵字串（單筆讀取如 123；匯出多筆時為 null）"),
                    RecordCount = table.Column<int>(type: "int", nullable: false, comment: "本次訪問涉及的資料筆數（單筆讀取通常為 1，匯出/下載為實際筆數）"),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true, comment: "來源用戶端 IP 位址（IPv4/IPv6；背景作業或無法取得時為 null）"),
                    InvolvesSensitiveData = table.Column<bool>(type: "bit", nullable: false, comment: "是否涉及敏感資料（被訪問實體含 @SensitiveData 標記欄位）"),
                    SensitiveFieldsAccessed = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "被訪問的敏感欄位名稱清單 JSON（如 [\"PasswordHash\",\"PasswordSalt\"]；僅記名不記值）"),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "訪問目的或原因說明（選填，供稽核追溯）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "訪問發生時間（本地時區；INSERT 時設定一次，列不會被更新）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "訪問者識別（帳號或系統標識）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysAccessLog", x => x.Id);
                },
                comment: "資料訪問紀錄（append-only）：記錄使用者對敏感資料的讀取/匯出/下載事件，僅記被訪問的敏感欄位名稱、不記機密值");

            migrationBuilder.CreateTable(
                name: "SysAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, comment: "主鍵：異動紀錄流水號（資料庫產生）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "受異動的實體型別名（如 User）"),
                    TableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "受異動的資料表名（如 SysUser）"),
                    PrimaryKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, comment: "目標資料列主鍵字串（複合鍵以 k=v;k=v 表示）"),
                    Operation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "操作類型：Insert=新增／Update=修改／Delete=刪除（含軟刪除轉態）"),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "改前整列快照 JSON（Insert 為 null；憑證/機密已遮罩）"),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "改後整列快照 JSON（Delete 為 null；憑證/機密已遮罩）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "異動發生時間（本地時區；INSERT 時設定一次，列不會被更新）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "操作者識別（帳號或系統標識）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysAuditLog", x => x.Id);
                },
                comment: "資料異動紀錄（append-only）：記錄各資料表寫入的改前/改後整列快照、操作者與時間");

            migrationBuilder.CreateTable(
                name: "SysRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：角色 ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "角色代碼（未刪除列唯一；SUPERADMIN 為內建保護角色）"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "角色顯示名稱"),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "角色說明（可空）"),
                    RoleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Custom", comment: "角色種類：System 系統管理員類（不可刪除）／Custom 一般使用者類"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false, comment: "樂觀併發權杖：防止並行設定權限時靜默覆蓋"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRole", x => x.Id);
                },
                comment: "角色（一組職務權限集合）");

            migrationBuilder.CreateTable(
                name: "SysTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：選單項目 ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）"),
                    ParentId = table.Column<int>(type: "int", nullable: true, comment: "父選單 ID（外鍵至 SysTask；NULL 表示頂層）"),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, comment: "選單顯示名稱（同一父層下唯一）"),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "對應的前端內部路由；純容器父項可為空"),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, comment: "選單圖示識別字（可空）"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, comment: "同層顯示排序（小到大）"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "是否啟用；停用則不顯示於任何使用者側邊選單"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysTask_SysTask_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SysTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "後台側邊選單項目（樹節點）");

            migrationBuilder.CreateTable(
                name: "SysUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：使用者 ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "對外識別碼（UUIDv7，API 一律使用此值，不公開整數主鍵；SPEC-064）"),
                    Account = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "登入帳號（未刪除列唯一；小寫正規化）"),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "使用者顯示名稱"),
                    PasswordHash = table.Column<string>(type: "nvarchar(88)", maxLength: 88, nullable: false, comment: "密碼雜湊（加鹽；MUST NOT 記錄於日誌，Principle XVII）"),
                    PasswordSalt = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "密碼雜湊用鹽值"),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "聯絡電話（選填；數字、空白與 + - ( ) #）"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "電子郵件（選填）"),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime", nullable: true, comment: "上次修改密碼時間（本地時間；NULL 表示上線前建立、以 CreateDate 代替）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysUser", x => x.Id);
                },
                comment: "後台使用者（操作者帳號）");

            migrationBuilder.CreateTable(
                name: "SysUserLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, comment: "主鍵：操作紀錄流水號（資料庫產生）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "功能代碼（如 PersonalData）"),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "動作（如 Modify）"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "結果：Success=成功／Failure=失敗"),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "操作內容訊息（不得含密碼）"),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true, comment: "來源用戶端 IP 位址（IPv4/IPv6；背景作業或無法取得時為 null）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "操作時間（本地時區；INSERT 時設定一次，列不會被更新）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "操作者帳號（無登入者時為 system／anonymous）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysUserLog", x => x.Id);
                },
                comment: "使用者操作紀錄（append-only）：記錄重要業務操作的成功／失敗，供『使用者存取記錄』查詢");

            migrationBuilder.CreateTable(
                name: "SysRoleTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：角色選單可見性 ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SysRoleId = table.Column<int>(type: "int", nullable: false, comment: "角色 ID（外鍵至 SysRole）"),
                    SysTaskId = table.Column<int>(type: "int", nullable: false, comment: "選單項目 ID（外鍵至 SysTask）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysRoleTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysRoleTask_SysRole_SysRoleId",
                        column: x => x.SysRoleId,
                        principalTable: "SysRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SysRoleTask_SysTask_SysTaskId",
                        column: x => x.SysTaskId,
                        principalTable: "SysTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "角色↔選單可見性對應（哪個角色可看到哪個選單）");

            migrationBuilder.CreateTable(
                name: "SysUserRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：使用者角色指派 ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "使用者 ID（外鍵至 SysUser）"),
                    RoleId = table.Column<int>(type: "int", nullable: false, comment: "角色 ID（外鍵至 SysRole）"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysUserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysUserRole_SysRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SysRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SysUserRole_SysUser_UserId",
                        column: x => x.UserId,
                        principalTable: "SysUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "使用者↔角色指派（多對多）");

            migrationBuilder.CreateTable(
                name: "SysUserSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, comment: "主鍵：session ID（強型別，資料庫 IDENTITY 配號）")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "對外識別碼（UUIDv7），即 access token 的 sid claim"),
                    UserId = table.Column<int>(type: "int", nullable: false, comment: "登入的使用者 ID（外鍵至 SysUser）"),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "目前 refresh token 的 SHA-256 雜湊（小寫十六進位；原值不落地）"),
                    PreviousTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "前一個（已輪替掉的）refresh token 雜湊，供重用偵測；從未輪替為 NULL"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false, comment: "到期時間（本地時間；建立或輪替時為當時＋8 小時）"),
                    LastRotatedAt = table.Column<DateTime>(type: "datetime", nullable: true, comment: "最後一次輪替時間（本地時間；從未輪替為 NULL）"),
                    RevokedAt = table.Column<DateTime>(type: "datetime", nullable: true, comment: "撤銷時間（本地時間；NULL 表示未撤銷）"),
                    RevokeReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "撤銷原因：Logout 登出、Reuse 重用已輪替的 refresh token、UserInvalid 帳號刪除或無角色"),
                    CreateDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "建立時間（本地時區；INSERT 時設定一次，之後不變）"),
                    CreateUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "建立者識別（使用者 ID 或帳號；INSERT 時設定一次）"),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: false, comment: "最近更新時間（本地時區；每次 UPDATE 刷新）"),
                    ModifyUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "最近更新者識別（每次 UPDATE 刷新）"),
                    Removed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false, comment: "軟刪除旗標：1=已邏輯刪除，0=有效")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysUserSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysUserSession_SysUser_UserId",
                        column: x => x.UserId,
                        principalTable: "SysUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "登入 session：一次登入一筆，保存目前 refresh token 的雜湊，每次換新即輪替（ADR-011）");

            migrationBuilder.CreateIndex(
                name: "IX_SysAccessLog_CreateUser_CreateDate",
                table: "SysAccessLog",
                columns: new[] { "CreateUser", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysAccessLog_EntityName_PrimaryKey_CreateDate",
                table: "SysAccessLog",
                columns: new[] { "EntityName", "PrimaryKey", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysAccessLog_InvolvesSensitiveData_CreateDate",
                table: "SysAccessLog",
                columns: new[] { "InvolvesSensitiveData", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysAuditLog_CreateUser_CreateDate",
                table: "SysAuditLog",
                columns: new[] { "CreateUser", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysAuditLog_EntityName_PrimaryKey_CreateDate",
                table: "SysAuditLog",
                columns: new[] { "EntityName", "PrimaryKey", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysRole_Code",
                table: "SysRole",
                column: "Code",
                unique: true,
                filter: "[Removed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SysRole_PublicId",
                table: "SysRole",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysRoleTask_SysRoleId_SysTaskId",
                table: "SysRoleTask",
                columns: new[] { "SysRoleId", "SysTaskId" },
                unique: true,
                filter: "[Removed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SysRoleTask_SysTaskId",
                table: "SysRoleTask",
                column: "SysTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SysTask_ParentId_Name",
                table: "SysTask",
                columns: new[] { "ParentId", "Name" },
                unique: true,
                filter: "[Removed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SysTask_PublicId",
                table: "SysTask",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysUser_Account",
                table: "SysUser",
                column: "Account",
                unique: true,
                filter: "[Removed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SysUser_PublicId",
                table: "SysUser",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysUserLog_CreateUser_CreateDate",
                table: "SysUserLog",
                columns: new[] { "CreateUser", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysUserLog_FunctionId_CreateDate",
                table: "SysUserLog",
                columns: new[] { "FunctionId", "CreateDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SysUserRole_RoleId",
                table: "SysUserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserRole_UserId_RoleId",
                table: "SysUserRole",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[Removed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserSession_PreviousTokenHash",
                table: "SysUserSession",
                column: "PreviousTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserSession_PublicId",
                table: "SysUserSession",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysUserSession_TokenHash",
                table: "SysUserSession",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SysUserSession_UserId",
                table: "SysUserSession",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SysAccessLog");

            migrationBuilder.DropTable(
                name: "SysAuditLog");

            migrationBuilder.DropTable(
                name: "SysRoleTask");

            migrationBuilder.DropTable(
                name: "SysUserLog");

            migrationBuilder.DropTable(
                name: "SysUserRole");

            migrationBuilder.DropTable(
                name: "SysUserSession");

            migrationBuilder.DropTable(
                name: "SysTask");

            migrationBuilder.DropTable(
                name: "SysRole");

            migrationBuilder.DropTable(
                name: "SysUser");
        }
    }
}
