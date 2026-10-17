# EF Core 入門：從一個需求推到資料庫

- 標籤：#ef-core #orm #database #sql-server #migration #code-first
- 最後更新：2026-10-17
- 圖解版：[ef-core-basics.html](ef-core-basics.html)（用瀏覽器開啟，含架構圖、時序圖、流程圖）

> 範例沿用微軟官方教學的 `Blog` / `Post` / `Tag` 模型，環境為 .NET 6 + EF Core 6 + SQL Server。

## 重點摘要

- **class（Entity）** 是程式端描述「資料長什麼樣子」的藍圖，一個 class 對應一張資料表
- **ORM（EF Core）** 負責「物件 ↔ 資料列」、「LINQ ↔ SQL」的自動翻譯
- `DbContext` = 一次工作階段（追蹤變更、`SaveChanges` 一次寫入）；`DbSet<T>` = 一張表
- 需要：`Microsoft.EntityFrameworkCore.SqlServer`、`Microsoft.EntityFrameworkCore.Design`（6.0.x）、`dotnet-ef` 工具
- 流程：Entity → DbContext → 連線字串 + `AddDbContext` → `dotnet ef migrations add` → `dotnet ef database update`

---

## 0. 故事的開始

任務：做一個**部落格平台後端 API**（開部落格、發文章、加標籤）。條件：**今天發的文章，後天還要在。**
這句話引出一連串問題，本文照這個順序推理：

```
要保存文章
  → ❶ 資料存哪裡？            → 資料庫
  → ❷ .NET 怎麼跟資料庫溝通？  → 驅動程式 + 連線字串，送 SQL、收表格
  → ❸ 收回的表格怎麼用？       → 定義 class 承接
  → ❹ 每次手寫 SQL / 轉換太累  → 交給 ORM（EF Core）
  → ❺ EF Core 內部怎麼運作？
  → ❻ 實際做一次：需求 → 分析 → 實作 → 驗收
```

## 1. 資料要存在哪裡？

| 需要的能力 | 記憶體變數（`List<Post>`） | JSON 檔案 | 關聯式資料庫 |
|-----------|--------------------------|-----------|-------------|
| 重啟後資料還在 | ✗ | ✓ | ✓ |
| 多人同時寫入 | ✗ | ✗ 互相覆蓋 | ✓ 鎖定 |
| 大量資料快速查詢 | — | ✗ 全讀進來比對 | ✓ 索引 |
| 寫到一半失敗不壞 | — | ✗ | ✓ 交易 |
| 保證資料關係正確 | ✗ | ✗ | ✓ 外鍵 |

結論：交給資料庫。資料以**資料表**存放：一列（row）一筆資料，一欄（column）一個欄位。

## 2. .NET 程式和資料庫是什麼關係？

兩個**獨立的程式**，透過網路對話：

```
[你的 .NET 程式] ── 驅動程式 Microsoft.Data.SqlClient ──①SQL 文字──▶ [SQL Server 引擎] ── 資料檔
                    （依連線字串找到資料庫）          ◀──②表格資料──
```

| 要素 | 說明 |
|------|------|
| 連線字串 | 資料庫位置、名稱、登入方式 |
| 驅動程式（ADO.NET Provider） | 開連線、送 SQL、收結果；SQL Server 用 `Microsoft.Data.SqlClient` |
| SQL | 資料庫唯一聽得懂的語言 |

**「收回表格」是什麼意思？** 資料庫不認識你的 C# class，執行 `SELECT` 後回傳的是一張**結果集（Result Set）**：

```
SELECT PostId, Title, PublishedOn FROM Posts WHERE BlogId = 1

 PostId (int) | Title (nvarchar) | PublishedOn (datetime2)
--------------+------------------+------------------------
 1            | EF Core 入門     | 2026-10-01 09:00        ◀ reader 目前指著這列
 2            | LINQ 小技巧      | 2026-10-05 14:30
 3            | 草稿             | NULL
```

- 欄 = SELECT 的欄位（帶資料庫型別），列 = 每筆符合的資料，沒值是 `NULL`（在 SSMS / Rider 執行 SQL 時下方那張表就是它）
- `SqlDataReader` 像游標：`Read()` 移到下一列，`reader["Title"]` 取目前列的某格，型別是 `object` 要自己轉
- `NULL` 讀出來是 `DBNull.Value`，不是 C# 的 `null`
- `INSERT` / `UPDATE` / `DELETE` 不回表格，只回「影響幾列」（`ExecuteNonQuery`）

最原始寫法 **ADO.NET**：`SqlConnection` 開連線 → `SqlCommand` 送 SQL（參數化）→ `SqlDataReader` 一列一列、用欄位名字串讀值並自己轉型。

**記住**：EF Core 底層也是 ADO.NET，永遠是「送 SQL 文字、收表格資料」。

## 3. 收回來的表格怎麼用？→ 需要 class

程式和資料庫是兩個不同的世界（Object-Relational Impedance Mismatch）：

| | C# 程式（物件世界） | 資料庫（關聯世界） |
|---|---|---|
| 資料單位 | 物件 `Blog` | 一列 row |
| 結構 | class 的屬性 | table 的欄位 |
| 關聯 | 物件參考 `blog.Posts` | 外鍵 `Posts.BlogId` |
| 操作 | 方法、LINQ | SQL 字串 |

沒有 class 時，用欄位名字串讀值，打錯字或轉錯型別要到執行才會發現：

```csharp
var url = (string)reader["Ulr"];   // 編譯會過，執行才爆炸
```

有 class 時，編譯器會檢查：

```csharp
var blog = await db.Blogs.FirstAsync(b => b.BlogId == 1);
Console.WriteLine(blog.Url);       // 打錯屬性名 → 編譯錯誤
```

class 的目的：

- **描述資料結構**：屬性 = 欄位，型別 = 欄位型別（`string` → `nvarchar(max)`、`DateTime?` → 可 NULL 的 `datetime2`）
- **型別安全**：編譯期檢查、IDE 自動完成
- **Code First 的設計圖**：EF Core 依 class 推導資料表，Migration 依此建表
- **用物件表達關聯**：`post.Blog.Url` 取代手寫 JOIN
- **在各層之間攜帶資料**：查詢結果是物件，可加工、序列化成 JSON
- **與資料庫解耦**：換資料庫只換 Provider

對應資料表的 class 稱為 **Entity**，不需繼承任何 EF 類別，也叫 **POCO**。

## 4. 每次手寫 SQL 和轉換太累 → 需要 ORM

ORM（Object-Relational Mapper）就是兩個世界的翻譯層。

| 工作 | 沒有 ORM | EF Core 自動處理 |
|------|---------|------------------|
| 寫 SQL | 手寫字串 | LINQ 轉譯 |
| 資料列 → 物件 | `reader["欄位"]` 逐一轉型 | 自動實體化 |
| 防 SQL Injection | 自己參數化 | 預設參數化 |
| 產生 UPDATE | 自己判斷改了什麼 | Change Tracker 比對 |
| 交易 | 手動 `BeginTransaction` | 一次 `SaveChanges` = 一個交易 |
| 資料表結構 | 手寫 DDL | Migration 產生並有版本紀錄 |
| 換資料庫 | 改寫 SQL 方言 | 換 Provider |

| | ADO.NET | Dapper（Micro-ORM） | EF Core（完整 ORM） |
|---|---|---|---|
| SQL 誰寫 | 自己 | 自己 | 自動（也可自己寫） |
| 結果 → 物件 | 手動 | ✓ | ✓ |
| 變更追蹤 / Migration | ✗ | ✗ | ✓ |
| 效能 | 最快 | 接近 ADO.NET | 稍慢 |
| 開發速度 | 慢 | 中 | 快 |
| 適合 | 極致效能 | 複雜報表、手調 SQL | 以 CRUD 與商業邏輯為主 |

ORM 的代價：

- 效能開銷：純讀取加 `AsNoTracking()`
- **N+1 問題**：迴圈中讀導覽屬性，每筆多一次查詢 → 用 `Include` 一次載入
- 還是要懂 SQL：Log 設定 `"Microsoft.EntityFrameworkCore.Database.Command": "Information"` 可看實際 SQL
- 可混用：`FromSqlInterpolated` 執行自寫 SQL，或搭配 Dapper

## 5. EF Core 的組成

```
API Endpoint ──DI 注入──▶ DbContext (DbSet<Blog>, DbSet<Post>)
                              │
                              ▼
                    EF Core 核心：LINQ 轉譯、Change Tracker、實體化
                              │
                              ▼
                    Provider（SqlServer）：產生 T-SQL ◀── 連線字串（appsettings.json）
                              │
                              ▼
                         SQL Server
```

| 角色 | 負責 | 對應資料庫 |
|------|------|-----------|
| Entity | 資料的形狀 | 一張 Table |
| `DbContext` | 工作階段：查詢入口、追蹤變更、`SaveChanges()` | 整個資料庫 |
| `DbSet<T>` | 查詢與新增的入口 | 一張 Table |
| Provider | 翻成特定資料庫的 SQL | 資料庫種類 |
| 連線字串 | 資料庫位置、登入方式 | 連線資訊 |
| Migration | 比對 class 變化產生建表 / 改表程式碼 | Schema 版本 |

## 6. class 如何對應資料表

| C# 端 | 資料庫端 |
|-------|---------|
| `Blog { BlogId, Url, List<Post> Posts }` | `Blogs (BlogId PK, Url)` |
| `Post { PostId, Title, Content, BlogId, Blog Blog, List<Tag> Tags }` | `Posts (PostId PK, Title, Content, BlogId FK)` |
| `Tag { TagId, Name, List<Post> Posts }` | `Tags (TagId PK, Name)` |
| `Post.Tags` ↔ `Tag.Posts`（多對多） | 中間表 `PostTag (PostsPostId, TagsTagId)` |

- **導覽屬性不會變成欄位**，資料庫只有外鍵
- **多對多**：兩邊各放 `List`，EF Core 5+ 自動建中間表
- **主鍵慣例**：`Id` 或 `<類別名>Id`；`int` 主鍵自動遞增

## 7. 查詢的背後

1. Request 進來，DI 建立本次 request 專用的 DbContext（Scoped）
2. `db.Blogs.Where(...)` 組成 LINQ 運算式樹（**此時還沒查資料庫**）
3. `ToListAsync()` 觸發執行：EF 轉譯成 SQL，Provider 送到資料庫
4. 資料列回來，EF 轉成 `Blog` 物件（實體化）並登記到 Change Tracker
5. 回傳 `List<Blog>`，序列化成 JSON
6. Request 結束，DbContext 被 Dispose，連線歸還連線池

**延遲執行**：`Where`、`OrderBy` 只是組查詢；`ToListAsync`、`FirstAsync`、`CountAsync` 才真正送出 SQL。

## 8. 寫入的背後

1. `db.Blogs.Add(blog)` → 標記為 `Added`（還沒寫入）
2. 查出的 `post` → `Unchanged`，並保存原始值
3. `post.Title = "..."` → 只改 C# 物件
4. `SaveChangesAsync()` → 比對原始值得出 `Modified` → 開交易 → `INSERT` / `UPDATE` → `COMMIT`
5. 取回自動遞增的 Id，所有 Entity 改回 `Unchanged`

| 狀態 | 何時 | SaveChanges 時 |
|------|------|---------------|
| `Added` | `Add()` | `INSERT` |
| `Unchanged` | 剛查詢 / 剛存檔 | 不做事 |
| `Modified` | 改了屬性 | `UPDATE`（只更新有變的欄位） |
| `Deleted` | `Remove()` | `DELETE` |
| `Detached` | 未被追蹤 | 不做事 |

## 9. Code First 開發流程

```
改 Entity / OnModelCreating
  → dotnet ef migrations add <名稱>
  → 檢查 Up() / Down()（不對就 dotnet ef migrations remove，回頭改）
  → dotnet ef database update
  → commit Entity + Migrations
```

- **Code First**：先寫 class，產生資料庫（新專案）
- **Database First**：已有資料庫，`dotnet ef dbcontext scaffold` 反向產生 class

---

## 10–11. 情境演練：需求單與分析

> 完整版（含分析圖、API 程式碼、時序圖、驗收表）見 [ef-core-basics.html](ef-core-basics.html) 第 10–13 節。

**需求：小型部落格平台 API**

- R1 部落格有網址，網址不可重複
- R2 部落格有多篇文章；標題必填、最多 200 字；有內容、發布時間（預設現在）
- R3 文章可有多個標籤，標籤可共用，名稱不可重複
- R4 刪除部落格時文章一併刪除
- R5 API：某部落格文章列表（新到舊）、依標籤查文章、新增文章時可給標籤（不存在就建立）

**分析四步驟**

| 步驟 | 問題 | 結果 | 對應實作 |
|------|------|------|---------|
| ① 找名詞 | 哪些有自己的身分？ | Blog、Post、Tag（網址、標題等只是屬性；標籤要共用、要被查詢 → 獨立 Entity） | Entity class |
| ② 找關係 | 幾對幾？ | Blog 1:N Post、Post N:N Tag | 導覽屬性 + 外鍵 |
| ③ 找規則 | 必填、長度、唯一、刪除、預設 | R1 `HasIndex(Url).IsUnique()`；R2 `IsRequired().HasMaxLength(200)`、`HasDefaultValueSql("getutcdate()")`；R3 `HasIndex(Name).IsUnique()`；R4 `OnDelete(Cascade)` | Fluent API |
| ④ 找操作 | 要查 / 寫什麼 | `POST /blogs`、`DELETE /blogs/{id}`、`GET /blogs/{id}/posts`、`GET /tags/{name}/posts`、`POST /blogs/{id}/posts` | Endpoint + LINQ |

**實作重點**

- 唯一性：程式先 `AnyAsync` 檢查回 409，資料庫唯一索引是最後防線（併發時程式檢查會漏）
- API 不直接回傳 Entity（導覽屬性循環 → `A possible object cycle was detected`），用 `Select` 投影
- 依標籤查：`db.Posts.Where(p => p.Tags.Any(t => t.Name == name))`
- 新增文章帶標籤：先查已存在的 Tag（`names.Contains(t.Name)` → SQL `IN`），不存在的 `new Tag`，一次 `SaveChanges` 在同一交易寫入 Posts、Tags、PostTag
- 輸入驗證（如標題長度）要在進資料庫前做並回 400，資料庫限制只是最後防線

**驗收**：每條需求要能指出實作位置與驗證方式（例：R1 同網址建兩次 → 第二次 409；R4 刪部落格 → 文章與 PostTag 一併消失）。

**需求變更（R6 文章瀏覽次數）**：屬性非 Entity → 加 `ViewCount` + `HasDefaultValue(0)` → `migrations add AddPostViewCount` → 確認 `Up()` 只有 `AddColumn` → `database update`。

---

## 12. 實作：從零連接資料庫

### 12.1 安裝套件與工具

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 6.0.*
dotnet tool install --global dotnet-ef --version 6.*
```

### 12.2 準備資料庫

| 選項 | 連線字串 |
|------|---------|
| SQL Server LocalDB（Windows） | `Server=(localdb)\mssqllocaldb;Database=Blogging;Trusted_Connection=True;` |
| SQL Server（Docker） | `Server=localhost,1433;Database=Blogging;User Id=sa;Password=…;TrustServerCertificate=True;` |
| SQLite | `Data Source=blogging.db`（改用 `…Sqlite` 套件與 `UseSqlite`） |

確認 LocalDB：`sqllocaldb info`

### 12.3 Entity

```csharp
public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }
    public List<Post> Posts { get; set; } = new();
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public int BlogId { get; set; }      // 外鍵
    public Blog Blog { get; set; }       // 導覽屬性
    public List<Tag> Tags { get; set; } = new();
}

public class Tag
{
    public int TagId { get; set; }
    public string Name { get; set; }
    public List<Post> Posts { get; set; } = new();
}
```

### 12.4 DbContext

```csharp
using Microsoft.EntityFrameworkCore;

public class BloggingContext : DbContext
{
    public BloggingContext(DbContextOptions<BloggingContext> options) : base(options) { }

    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(eb =>
        {
            eb.Property(b => b.Url).IsRequired().HasMaxLength(500);
            eb.HasMany(b => b.Posts).WithOne(p => p.Blog).HasForeignKey(p => p.BlogId);
        });

        modelBuilder.Entity<Post>(eb =>
        {
            eb.Property(p => p.Title).IsRequired().HasMaxLength(200);
            eb.HasMany(p => p.Tags).WithMany(t => t.Posts);
        });
    }
}
```

建構子接收 `options`：連線字串與資料庫種類由 DI 註冊時決定，測試時可替換。

### 12.5 連線字串與註冊

```json
{
  "ConnectionStrings": {
    "BloggingDb": "Server=(localdb)\\mssqllocaldb;Database=Blogging;Trusted_Connection=True;"
  }
}
```

```csharp
builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BloggingDb")));
```

- JSON 中 `\` 要寫成 `\\`
- 含密碼的連線字串用 User Secrets，不要 commit
- `AddDbContext` 預設 **Scoped**；DbContext 非 thread-safe，不可 Singleton

### 12.6 Migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

產生 `<時間戳>_InitialCreate.cs`（Up/Down）、`.Designer.cs`、`BloggingContextModelSnapshot.cs`，都要 commit。

### 12.7 在 API 中使用

```csharp
app.MapGet("/blogs", async (BloggingContext db) =>
    await db.Blogs.AsNoTracking().Include(b => b.Posts).ToListAsync());

app.MapPost("/blogs", async (BloggingContext db, Blog blog) =>
{
    db.Blogs.Add(blog);
    await db.SaveChangesAsync();
    return Results.Created($"/blogs/{blog.BlogId}", blog);
});
```

## 14. 慣例與 Fluent API

| 慣例 | 效果 |
|------|------|
| `Id` / `<類別名>Id` | 主鍵，`int` 自動遞增 |
| `<導覽屬性名>Id` 且型別同對方主鍵 | 外鍵 |
| 兩邊集合導覽 | 多對多 + 中間表 |
| `int?`、`DateTime?` | 可為 NULL |
| `DbSet` 屬性名 | 資料表名 |

Data Annotations（`[Required]`、`[MaxLength]`）寫在 class 上；Fluent API 集中在 `OnModelCreating`，能力較完整，衝突時 **Fluent API 優先**。

| Fluent API | 效果 |
|------------|------|
| `HasKey` | 主鍵 |
| `IsRequired()` | NOT NULL |
| `HasMaxLength(n)` | `nvarchar(n)` |
| `HasColumnType` / `HasColumnName` | SQL 型別 / 欄位名稱 |
| `HasPrecision(18, 2)` | decimal 精度 |
| `HasDefaultValue` / `HasDefaultValueSql` | 預設值 |
| `HasIndex(...).IsUnique()` | 唯一索引 |
| `HasOne/HasMany/WithOne/WithMany` | 關聯 |
| `OnDelete(DeleteBehavior.X)` | 刪除行為 |

## 15. Migration 指令

| 指令 | 用途 |
|------|------|
| `dotnet ef migrations add <名稱>` | 新增 Migration |
| `dotnet ef database update [<名稱>]` | 套用到最新 / 指定版本 |
| `dotnet ef migrations remove` | 刪除最後一個未套用的 Migration |
| `dotnet ef migrations list` | 列出 Migration |
| `dotnet ef migrations script` | 產生 SQL 腳本 |
| `dotnet ef database drop` | 刪除資料庫 |
| `dotnet ef dbcontext scaffold "<連線字串>" Microsoft.EntityFrameworkCore.SqlServer` | Database First |

套用前檢查 `Up()`：`DropColumn` / `DropTable` 會失去資料；屬性改名可能被誤判成刪除再新增，要改成 `RenameColumn`。

## 16. 常見錯誤

| 錯誤訊息 | 原因 | 解法 |
|---------|------|------|
| `The entity type 'X' requires a primary key to be defined` | 沒有主鍵 | 加 `Id` 或 `HasKey` |
| `No store type was specified for the decimal property` | decimal 沒精度 | `HasPrecision(18, 2)` |
| `dotnet-ef does not exist` | 沒裝工具 | `dotnet tool install --global dotnet-ef` |
| `Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design` | 缺 Design 套件 | 安裝套件 |
| `The Entity Framework tools version 'X' is older than that of the runtime 'Y'` | 版本不一致 | `dotnet tool update --global dotnet-ef --version 6.*` |
| `A network-related or instance-specific error occurred` | 連不到資料庫 | `sqllocaldb info`、檢查連線字串 |
| `A possible object cycle was detected` | API 回傳含導覽屬性的 Entity | 用 `Select` 投影成 DTO |
| `Cannot insert duplicate key row ... with unique index` | 違反唯一索引 | 寫入前檢查並回 409 |
| `Introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths` | 多條串聯刪除路徑 | 其中一條 `.OnDelete(DeleteBehavior.NoAction)` |

## 參考資料

- [EF Core 入門教學](https://learn.microsoft.com/ef/core/get-started/overview/first-app)
- [建立模型](https://learn.microsoft.com/ef/core/modeling/)
- [關聯設定](https://learn.microsoft.com/ef/core/modeling/relationships)
- [變更追蹤](https://learn.microsoft.com/ef/core/change-tracking/)
- [Migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [dotnet ef 指令](https://learn.microsoft.com/ef/core/cli/dotnet)
- [效能指南](https://learn.microsoft.com/ef/core/performance/)
