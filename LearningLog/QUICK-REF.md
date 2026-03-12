# 快速參考指南

> 常見問題的快速解決方案

**最後更新：** 2026-03-12

---

## 🚨 常見錯誤速查

### Entity Framework Core

#### ❌ DbContext 建構函式錯誤
```
Error: There is no argument given that corresponds to the required 
parameter 'options' of 'DbContext.DbContext(DbContextOptions)'
```

**快速解決：**
```csharp
public class MyBoardsContext : DbContext
{
    // ✅ 加上這個建構函式
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
        : base(options)
    {
    }
}
```

**詳細說明：** [DbContext 建構函式問題](2026/03-March/2026-03-12-dbcontext-constructor.md)

---

#### ❌ DI 容器無法解析 DbContext
```
Error: Unable to resolve service for type 'MyBoardsContext'
```

**快速解決：**
在 `Program.cs` 加上：
```csharp
builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    options.UseSqlServer("連接字串");
});
```

**詳細說明：** [DbContext 建構函式問題](2026/03-March/2026-03-12-dbcontext-constructor.md)

---

### C# 語言

#### ❌ 子類別建構函式錯誤
```
Error: There is no argument given that corresponds to the required 
parameter of the base class constructor
```

**快速解決：**
```csharp
public class Child : Parent
{
    // ✅ 使用 : base(參數) 呼叫父類別建構函式
    public Child(string value) : base(value)
    {
    }
}
```

---

### ASP.NET Core

#### ❌ 注入失敗
```
Error: Cannot resolve service
```

**檢查清單：**
1. [ ] 是否在 `Program.cs` 註冊服務？
2. [ ] 生命週期設定正確嗎？（Singleton/Scoped/Transient）
3. [ ] 建構函式參數是否正確？

---

## 🔧 常用程式碼片段

### Entity Framework Core

#### 基本 DbContext 設定
```csharp
// Program.cs
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
    
    // 開發環境設定
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine);
    }
});
```

#### DbContext 類別範本
```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<Entity> Entities { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // 配置實體關係
    }
}
```

---

### Dependency Injection

#### Controller 注入
```csharp
public class MyController : ControllerBase
{
    private readonly MyDbContext _context;
    
    public MyController(MyDbContext context)
    {
        _context = context;
    }
}
```

#### Service 註冊
```csharp
// Singleton - 整個應用程式生命週期共用一個實例
builder.Services.AddSingleton<IMyService, MyService>();

// Scoped - 每個 HTTP 請求一個實例
builder.Services.AddScoped<IMyService, MyService>();

// Transient - 每次注入都建立新實例
builder.Services.AddTransient<IMyService, MyService>();
```

---

## 📋 檢查清單

### 新增 Entity 類別時
- [ ] 建立 Entity 類別
- [ ] 在 DbContext 加上 `DbSet<Entity>` 屬性
- [ ] 執行 Migration: `Add-Migration AddEntity`
- [ ] 更新資料庫: `Update-Database`

### 設定新的 DbContext 時
- [ ] 建立 DbContext 類別並繼承 `DbContext`
- [ ] 加上建構函式接收 `DbContextOptions`
- [ ] 在 Program.cs 註冊 `AddDbContext`
- [ ] 設定連接字串
- [ ] 執行初始 Migration

### 遇到問題時
- [ ] 檢查錯誤訊息
- [ ] 查看 [INDEX.md](INDEX.md) 是否有類似問題
- [ ] 搜尋本快速參考
- [ ] 記錄新問題到學習日誌

---

## 🔗 有用的命令

### Entity Framework Core

```powershell
# 新增 Migration
dotnet ef migrations add MigrationName

# 更新資料庫
dotnet ef database update

# 移除最後一個 Migration
dotnet ef migrations remove

# 查看 Migration 清單
dotnet ef migrations list

# 產生 SQL 腳本
dotnet ef migrations script
```

### .NET CLI

```powershell
# 建立新專案
dotnet new webapi -n ProjectName

# 加入套件
dotnet add package PackageName

# 還原套件
dotnet restore

# 建置專案
dotnet build

# 執行專案
dotnet run
```

---

## 📚 學習資源連結

### 官方文件
- [Entity Framework Core 文件](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core 文件](https://docs.microsoft.com/aspnet/core/)
- [C# 文件](https://docs.microsoft.com/dotnet/csharp/)

### 社群資源
- [Stack Overflow - ef-core tag](https://stackoverflow.com/questions/tagged/entity-framework-core)
- [Stack Overflow - asp.net-core tag](https://stackoverflow.com/questions/tagged/asp.net-core)

---

## 💡 除錯技巧

### 檢查 SQL 查詢
```csharp
// 在 DbContext 配置中加上
options.LogTo(Console.WriteLine, LogLevel.Information);
```

### 顯示敏感資料（僅開發環境）
```csharp
if (builder.Environment.IsDevelopment())
{
    options.EnableSensitiveDataLogging();
}
```

### 檢查 DI 註冊
在 Controller 建構函式設中斷點，確認服務是否正確注入

---

**維護者：** s9740  
**建立日期：** 2026-03-12

---

> 💡 **提示：** 這個檔案會隨著你累積更多經驗而不斷更新。每次解決問題後，考慮將解決方案加到這裡！
