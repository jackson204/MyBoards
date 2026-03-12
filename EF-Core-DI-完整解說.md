# Entity Framework Core 與 DI 完整解說

> 從零開始理解為什麼 DbContext 需要建構函式以及 DI 如何運作

---

## 目錄

1. [建構函式（Constructor）基礎](#1-建構函式constructor基礎)
2. [類別繼承與建構函式](#2-類別繼承與建構函式)
3. [什麼是 DI（相依性注入）](#3-什麼是-di相依性注入)
4. [Lambda 表達式入門](#4-lambda-表達式入門)
5. [DbContext 與 DbContextOptions](#5-dbcontext-與-dbcontextoptions)
6. [AddDbContext 的運作原理](#6-adddbcontext-的運作原理)
7. [完整執行流程圖解](#7-完整執行流程圖解)
8. [為什麼建構函式是必須的](#8-為什麼建構函式是必須的)
9. [常見問題 Q&A](#9-常見問題-qa)

---

## 1. 建構函式（Constructor）基礎

### 1.1 什麼是建構函式？

建構函式是「建立物件時會自動執行的特殊方法」。

```csharp
// 定義一個類別：車子
public class Car
{
    public string Color;  // 車子的顏色
    
    // 這就是建構函式！
    // 建構函式的名字必須和類別名字一樣
    public Car(string color)  // 當別人要做一台車時，必須告訴我顏色
    {
        Color = color;  // 把傳進來的顏色存起來
    }
}

// 使用時：
var myCar = new Car("紅色");  // 建立車子，傳入「紅色」
// myCar.Color 現在是 "紅色"
```

**重點：**
- 建構函式名字必須和類別名字一樣（`Car` 類別 → `Car` 建構函式）
- 當你寫 `new Car("紅色")` 時，就是在呼叫建構函式
- 建構函式可以要求參數，強制建立物件時必須提供某些值

### 1.2 有無建構函式的差異

#### 情況 A：沒寫建構函式

```csharp
public class Car
{
    public string Color;
    // 沒有建構函式！
}

// 使用時：
var myCar = new Car();  // C# 自動給你一個「空的建構函式」
myCar.Color = "紅色";   // 要自己手動設定
```

**問題：**
- ❌ 容易忘記設定，`Color` 可能是 `null`
- ❌ 無法強制要求「每台車一定要有顏色」

#### 情況 B：有建構函式要求參數

```csharp
public class Car
{
    public string Color;
    
    public Car(string color)  // 強制要求傳入顏色
    {
        Color = color;
    }
}

// 使用時：
var myCar = new Car();        // ❌ 編譯錯誤！必須傳入顏色
var myCar = new Car("紅色");  // ✅ 正確！
```

**優點：**
- ✅ 強制要求必要的資訊
- ✅ 保證物件建立時處於有效狀態

---

## 2. 類別繼承與建構函式

### 2.1 繼承的基本概念

```csharp
// 爸爸類別：交通工具
public class Vehicle
{
    public string Brand;  // 品牌
    
    // 爸爸的建構函式：要求提供品牌
    public Vehicle(string brand)
    {
        Brand = brand;
        Console.WriteLine($"建立了 {brand} 交通工具");
    }
}

// 孩子類別：汽車
public class Car : Vehicle  // Car 繼承 Vehicle
{
    public string Color;
    
    // 孩子的建構函式
    public Car(string brand, string color)
        : base(brand)  // ⚠️ 重點！呼叫爸爸的建構函式
    {
        Color = color;
        Console.WriteLine($"顏色是 {color}");
    }
}

// 使用：
var myCar = new Car("Toyota", "紅色");

// 輸出：
// 建立了 Toyota 交通工具  ← 爸爸的建構函式先執行
// 顏色是 紅色             ← 然後孩子的建構函式執行
```

### 2.2 執行順序詳解

當執行 `new Car("Toyota", "紅色")` 時：

```
Step 1: 進入 Car 的建構函式
        收到參數：brand = "Toyota", color = "紅色"

Step 2: 看到 : base(brand)
        把 "Toyota" 傳給爸爸 Vehicle

Step 3: 跳到 Vehicle 的建構函式
        收到 brand = "Toyota"
        執行：Brand = brand;
        輸出：建立了 Toyota 交通工具

Step 4: 回到 Car 的建構函式本體 { }
        執行：Color = color;
        輸出：顏色是 紅色

Step 5: 完成！物件建立好了
```

### 2.3 如果不呼叫父類別建構函式會怎樣？

```csharp
public class Vehicle
{
    public Vehicle(string brand)  // 爸爸要求必須提供 brand
    {
        //...
    }
}

public class Car : Vehicle
{
    public Car(string color)
    // ❌ 沒有 : base(...)
    {
        Color = color;
    }
}

// 編譯錯誤！
// 錯誤訊息：
// There is no argument given that corresponds to the required 
// parameter 'brand' of 'Vehicle.Vehicle(string)'
```

**C# 的規則：**
```
如果父類別有「需要參數」的建構函式：
└─ 子類別必須在建構函式中呼叫 : base(參數)
   否則 C# 不知道要怎麼建立父類別的那部分
```

---

## 3. 什麼是 DI（相依性注入）

### 3.1 傳統方式 vs DI 方式

#### ❌ 傳統方式（沒有 DI）

```csharp
// 在舊式的程式碼中
public class OrderController
{
    public void CreateOrder()
    {
        // 自己手動建立需要的物件
        var options = new DbContextOptionsBuilder<MyBoardsContext>()
            .UseSqlServer("Server=...; Database=...")
            .Options;
            
        var context = new MyBoardsContext(options);  // 自己 new
        
        // 使用 context...
        var orders = context.Orders.ToList();
        
        context.Dispose();  // 自己清理
    }
}
```

**問題：**
- ❌ 每個地方都要自己建立物件
- ❌ 連接字串寫死在程式碼裡
- ❌ 難以測試（無法替換成假的資料庫）
- ❌ 要自己管理物件生命週期
- ❌ 修改資料庫設定要改很多地方

#### ✅ 使用 DI（現代方式）

```csharp
public class OrderController
{
    private readonly MyBoardsContext _context;
    
    // 建構函式說：「我需要 MyBoardsContext」
    public OrderController(MyBoardsContext context)
    {
        _context = context;  // ASP.NET Core 自動給我！
    }
    
    public void CreateOrder()
    {
        // 直接使用，不用自己建立
        var orders = _context.Orders.ToList();
        _context.SaveChanges();
        // 不用自己 Dispose，框架會處理
    }
}
```

**優點：**
- ✅ 不用自己建立物件
- ✅ 設定集中管理
- ✅ 容易測試
- ✅ 自動管理生命週期
- ✅ 修改設定只要改一個地方

### 3.2 DI 容器的工作原理

想像 **DI 容器**是一個「智慧工廠」：

```
┌─────────────────────────────────────────────┐
│         DI 容器（服務工廠）                    │
│                                             │
│  註冊表（食譜書）：                            │
│  ┌───────────────────────────────────┐      │
│  │ MyBoardsContext                   │      │
│  │ ├─ 需要：DbContextOptions         │      │
│  │ ├─ 生命週期：Scoped（每個請求一個）│      │
│  │ └─ 建立方式：呼叫建構函式         │      │
│  │                                   │      │
│  │ DbContextOptions                  │      │
│  │ ├─ 資料庫：SQL Server             │      │
│  │ └─ 連接字串：Server=...           │      │
│  └───────────────────────────────────┘      │
│                                             │
│  當有人要 MyBoardsContext 時：               │
│  1. 查註冊表                                 │
│  2. 看到需要 DbContextOptions               │
│  3. 先建立 DbContextOptions                 │
│  4. 用 DbContextOptions 建立 MyBoardsContext│
│  5. 把建立好的物件給呼叫者                   │
└─────────────────────────────────────────────┘
```

---

## 4. Lambda 表達式入門

### 4.1 什麼是 Lambda？

Lambda 表達式是**匿名函式**（沒有名字的函式）。

```csharp
// 傳統方法：定義一個函式
public int Add(int a, int b)
{
    return a + b;
}

int result = Add(3, 5);  // 結果：8

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// Lambda 表達式：匿名函式（沒有名字的函式）
Func<int, int, int> add = (a, b) => a + b;
//                        ^^^^^^^^  ^^^^^
//                        參數      回傳值

int result = add(3, 5);  // 結果：8
```

**Lambda 的結構：**
```
(參數1, 參數2, ...) => 表達式或程式碼區塊
  ^^^^^^^^^^^^^       ^^^^^^^^^^^^^^^^^^^
  輸入                輸出或動作
```

### 4.2 更多 Lambda 範例

```csharp
// 例子 1：沒有參數
Action sayHello = () => Console.WriteLine("Hello");
//                ^^    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                沒參數 執行這個動作

sayHello();  // 輸出：Hello

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// 例子 2：一個參數
Action<string> greet = (name) => Console.WriteLine($"Hello {name}");
//                     ^^^^^^    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                     參數 name  印出 Hello + name

greet("Tom");  // 輸出：Hello Tom

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// 例子 3：多行程式碼要用 { }
Action<string> greetWithTime = (name) =>
{
    var time = DateTime.Now;
    Console.WriteLine($"Hello {name}");
    Console.WriteLine($"Current time: {time}");
};

greetWithTime("Tom");
// 輸出：
// Hello Tom
// Current time: 2026/3/12 10:30:00
```

---

## 5. DbContext 與 DbContextOptions

### 5.1 DbContext 是什麼？

`DbContext` 是 Entity Framework Core 提供的基礎類別，用於：
- 連接資料庫
- 執行查詢
- 追蹤變更
- 儲存資料

```csharp
// DbContext 是微軟寫好的類別（父類別）
public abstract class DbContext  // 這是微軟提供的
{
    // DbContext 的建構函式要求必須提供 options
    protected DbContext(DbContextOptions options)
    {
        // 用 options 初始化資料庫連接
        // 設定要連哪個資料庫
        // 設定連接字串
        // 等等...
    }
}

// MyBoardsContext 是你的類別（子類別）
public class MyBoardsContext : DbContext  // 繼承 DbContext
{
    // ⚠️ 問題來了！
    // 父類別（DbContext）的建構函式需要 options
    // 所以子類別必須提供 options 給父類別
}
```

### 5.2 DbContextOptions 包含什麼？

`DbContextOptions` 是一個配置物件，包含：

```csharp
DbContextOptions
├─ 資料庫提供者
│  └─ SQL Server / SQLite / PostgreSQL / MySQL / In-Memory
│
├─ 連接資訊
│  └─ 連接字串（伺服器位址、資料庫名稱、帳密等）
│
├─ 行為設定
│  ├─ 查詢追蹤行為
│  ├─ 命令逾時時間
│  └─ 批次處理大小
│
├─ 日誌設定
│  ├─ 是否記錄 SQL
│  ├─ 記錄層級
│  └─ 是否顯示敏感資料
│
└─ 內部服務
   ├─ 連接管理器
   ├─ 命令執行器
   └─ 查詢編譯器
```

### 5.3 為什麼需要建構函式？

```csharp
// ❌ 沒有建構函式
public class MyBoardsContext : DbContext
{
    // 沒有建構函式！
    public DbSet<WorkItem> WorkItems { get; set; }
}

// C# 編譯器會說：
// ❌ 錯誤！
// DbContext 沒有無參數的建構函式
// 你必須明確呼叫 DbContext 的建構函式並傳入 DbContextOptions

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// ✅ 正確的寫法
public class MyBoardsContext : DbContext
{
    // 子類別的建構函式
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
        : base(options)  // 把 options 傳給父類別 DbContext
    {
        // 本體通常是空的，因為父類別會處理一切
    }
    
    public DbSet<WorkItem> WorkItems { get; set; }
}
```

---

## 6. AddDbContext 的運作原理

### 6.1 基本用法

```csharp
// Program.cs
builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    options.UseSqlServer("Server=localhost; Database=MyBoards;");
});
```

### 6.2 AddDbContext 做了什麼？

```csharp
// AddDbContext 的簡化實作
public static IServiceCollection AddDbContext<TContext>(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> optionsAction  // ← 你的 lambda
)
where TContext : DbContext
{
    // 註冊到 DI 容器
    services.AddScoped<TContext>(serviceProvider =>
    {
        // 1. 建立 options builder
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        
        // 2. 執行你傳入的 lambda 來配置 options
        optionsAction(optionsBuilder);
        //            ^^^^^^^^^^^^^^
        //            把 builder 當作 options 參數傳給你的 lambda
        
        // 3. 取得配置好的 options
        var options = optionsBuilder.Options;
        
        // 4. 用 options 建立 DbContext
        // ⚠️ 這裡需要建構函式！
        return (TContext)Activator.CreateInstance(typeof(TContext), options);
    });
    
    return services;
}
```

### 6.3 Lambda 參數從哪來？

```csharp
// 你的程式碼
builder.Services.AddDbContext<MyBoardsContext>(options =>
//                                             ^^^^^^^ 
//                                             這個參數是誰給的？
{
    options.UseSqlServer("...");
});

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// AddDbContext 內部執行：
var optionsBuilder = new DbContextOptionsBuilder<MyBoardsContext>();
//  ^^^^^^^^^^^^^^
//  這就是等下要傳給你的 options

optionsAction(optionsBuilder);
//            ^^^^^^^^^^^^^^
//            傳給你的 lambda！

// 所以你的 lambda 中的 options = optionsBuilder
```

### 6.4 等價的寫法比較

```csharp
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 寫法 A：Lambda（最常用）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    options.UseSqlServer("Server=localhost; Database=MyBoards;");
});


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 寫法 B：定義方法（展開版）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

void ConfigureDatabase(DbContextOptionsBuilder options)
{
    options.UseSqlServer("Server=localhost; Database=MyBoards;");
}

builder.Services.AddDbContext<MyBoardsContext>(ConfigureDatabase);


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 寫法 C：完全手動（完整展開）
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

builder.Services.AddScoped<MyBoardsContext>(serviceProvider =>
{
    var optionsBuilder = new DbContextOptionsBuilder<MyBoardsContext>();
    optionsBuilder.UseSqlServer("Server=localhost; Database=MyBoards;");
    var options = optionsBuilder.Options;
    
    // ⚠️ 這裡需要建構函式！
    return new MyBoardsContext(options);
});

// 三種寫法完全等價！
```

---

## 7. 完整執行流程圖解

### 7.1 階段概覽

```
應用程式啟動（Program.cs）
    ↓
註冊服務到 DI 容器
    ↓
應用程式運行（等待請求）
    ↓
HTTP 請求進來
    ↓
Controller 需要 MyBoardsContext
    ↓
DI 容器建立 MyBoardsContext
    ↓
執行 Lambda 配置 options
    ↓
呼叫 MyBoardsContext 建構函式
    ↓
DbContext 初始化
    ↓
注入到 Controller
    ↓
執行業務邏輯
    ↓
請求結束，清理資源
```

### 7.2 詳細流程

#### 🔵 階段 1：應用程式啟動（Program.cs）

```csharp
var builder = WebApplication.CreateBuilder(args);

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 📋 註冊服務到 DI 容器
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    options.UseSqlServer("Server=localhost; Database=MyBoards;");
});

// 這時候：
// ✓ Lambda 被儲存到 DI 容器
// ✗ Lambda 還沒執行
// ✗ 還沒建立 MyBoardsContext
// ✗ 還沒連接資料庫

var app = builder.Build();
app.Run();
```

#### 🟢 階段 2：HTTP 請求進來

```
使用者瀏覽器
    │
    │ HTTP GET /api/workitems
    ↓
┌─────────────────────────────────────┐
│   ASP.NET Core 管線                 │
│                                     │
│   1. 接收請求                        │
│   2. 路由分析                        │
│   3. 找到對應的 Controller           │
│      → WorkItemsController          │
│   4. 準備建立 Controller 實例        │
└─────────────────────────────────────┘
```

#### 🟡 階段 3：Controller 需要 MyBoardsContext

```csharp
// WorkItemsController.cs
public class WorkItemsController : ControllerBase
{
    private readonly MyBoardsContext _context;
    
    // 建構函式注入
    public WorkItemsController(MyBoardsContext context)
    //                         ^^^^^^^^^^^^^^^^^^
    //                         需要 MyBoardsContext
    {
        _context = context;
    }
}

// ASP.NET Core：
// 「要建立 WorkItemsController」
// 「建構函式需要 MyBoardsContext」
// 「去 DI 容器要...」
```

#### 🔴 階段 4：DI 容器建立 MyBoardsContext

```
DI 容器：
「有人要 MyBoardsContext！」
「查看註冊表...找到了！」
「看看要怎麼建立...」

┌────────────────────────────────────────────┐
│ Step 1: 建立 DbContextOptionsBuilder       │
└────────────────────────────────────────────┘
var optionsBuilder = new DbContextOptionsBuilder<MyBoardsContext>();

此時 optionsBuilder 是空的，還沒有任何設定

                  ↓

┌────────────────────────────────────────────┐
│ Step 2: 執行你的 Lambda 配置 options       │
└────────────────────────────────────────────┘
optionsAction(optionsBuilder);

// 你的 lambda 執行：
options =>
{
    options.UseSqlServer("Server=localhost; Database=MyBoards;");
}

// options 參數 = optionsBuilder
// 所以實際上執行：
optionsBuilder.UseSqlServer("Server=localhost; Database=MyBoards;");

                  ↓

┌────────────────────────────────────────────┐
│ Step 3: UseSqlServer 內部做了什麼          │
└────────────────────────────────────────────┘
optionsBuilder.UseSqlServer(...) 執行：
├─ 註冊 SQL Server 提供者
├─ 儲存連接字串
├─ 設定 SQL Server 服務
├─ 配置連接池
└─ 設定命令逾時

現在 optionsBuilder 包含了完整的資料庫設定

                  ↓

┌────────────────────────────────────────────┐
│ Step 4: 取得配置好的 options               │
└────────────────────────────────────────────┘
var options = optionsBuilder.Options;

options 現在包含：
├─ Provider: SQL Server
├─ ConnectionString: Server=localhost; Database=MyBoards;
├─ Services: 連接管理、命令執行等
└─ Configuration: 各種設定

                  ↓

┌────────────────────────────────────────────┐
│ Step 5: 呼叫 MyBoardsContext 建構函式      │
└────────────────────────────────────────────┘
return new MyBoardsContext(options);

⚠️ 如果 MyBoardsContext 沒有這個建構函式
   這行程式碼會編譯失敗！
```

#### 🟣 階段 5：MyBoardsContext 建構函式執行

```csharp
// MyBoardsContext.cs
public class MyBoardsContext : DbContext
{
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
    //                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                     收到 DI 容器傳來的 options
        : base(options)  // 把 options 傳給父類別 DbContext
    {
        // 本體通常是空的
    }
}

執行流程：
1. 進入 MyBoardsContext 建構函式
2. 執行 : base(options)，跳到 DbContext 建構函式
3. DbContext 用 options 初始化：
   ├─ 建立資料庫連接
   ├─ 初始化變更追蹤器
   ├─ 準備查詢編譯器
   └─ 設定 DbSet 屬性
4. 回到 MyBoardsContext 建構函式本體（空的）
5. 完成！MyBoardsContext 已準備好使用
```

#### 🟢 階段 6：注入到 Controller

```csharp
DI 容器：
「MyBoardsContext 建立好了！」
「現在建立 WorkItemsController」

var context = /* 剛建立的 MyBoardsContext */;
var controller = new WorkItemsController(context);
//                                       ^^^^^^^
//                                       注入！

Controller 建構函式執行：
public WorkItemsController(MyBoardsContext context)
{
    _context = context;  // 儲存起來，準備使用
}
```

#### 🟡 階段 7：執行業務邏輯

```csharp
[HttpGet]
public async Task<IActionResult> GetAll()
{
    // 使用注入的 _context
    var items = await _context.WorkItems.ToList();
    
    // EF Core 會：
    // 1. 使用 options 中的連接字串連接資料庫
    // 2. 執行 SQL: SELECT * FROM WorkItems
    // 3. 把結果轉成 WorkItem 物件
    // 4. 追蹤變更（如果需要）
    
    return Ok(items);
}
```

#### 🟤 階段 8：請求結束，清理資源

```
HTTP 請求處理完成

↓

ASP.NET Core：
「這個請求的 Scope 結束了」
「該清理這個 Scope 內建立的物件」

↓

DI 容器：
「找到這個請求建立的 MyBoardsContext」
「呼叫 context.Dispose()」

↓

Dispose 執行：
├─ 關閉資料庫連接
├─ 清理變更追蹤器
├─ 釋放記憶體
└─ 清理其他資源

✅ 資源清理完成！
```

---

## 8. 為什麼建構函式是必須的

### 8.1 原因 1：C# 語言規則

```csharp
// 父類別（DbContext）
public abstract class DbContext
{
    protected DbContext(DbContextOptions options)  // 需要 options
    {
        // 用 options 初始化
    }
    
    // ⚠️ 沒有無參數建構函式！
}

// 子類別（MyBoardsContext）
public class MyBoardsContext : DbContext
{
    // 如果不寫建構函式，C# 會自動嘗試：
    // public MyBoardsContext() : base() { }
    //                            ^^^^^^
    //                            找不到 base() 無參數建構函式
    //                            ❌ 編譯錯誤！
    
    // 正確寫法：明確呼叫父類別的建構函式
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
        : base(options)  // 傳 options 給父類別
    {
    }
}
```

**結論：因為 DbContext 需要 options，所以子類別必須提供方法來傳入 options**

### 8.2 原因 2：DI 容器需要知道怎麼建立物件

```csharp
// DI 容器的邏輯（簡化）
public class DIContainer
{
    public T CreateInstance<T>()
    {
        // 1. 檢查 T 的建構函式
        var constructor = typeof(T).GetConstructors()[0];
        
        // 2. 取得建構函式的參數
        var parameters = constructor.GetParameters();
        
        // 3. 為每個參數準備值
        object[] args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = GetService(parameters[i].ParameterType);
            //        ^^^^^^^^^^^ 從註冊表中取得
        }
        
        // 4. 呼叫建構函式建立物件
        return (T)constructor.Invoke(args);
        //            ^^^^^^^^^^^^^^
        //            如果沒有建構函式，這裡會失敗！
    }
}
```

**結論：沒有建構函式，DI 容器就不知道怎麼傳入 options**

### 8.3 原因 3：DbContext 需要 options 才能運作

```csharp
public abstract class DbContext
{
    private readonly DbContextOptions _options;
    
    protected DbContext(DbContextOptions options)
    {
        _options = options;
        
        // options 包含：
        // - 資料庫提供者（SQL Server、SQLite等）
        // - 連接字串
        // - 服務提供者
        
        // 沒有 options 就：
        // ❌ 不知道要連哪個資料庫
        // ❌ 不知道連接字串
        // ❌ 無法執行查詢
        // ❌ 無法儲存資料
    }
    
    public DbSet<T> Set<T>() where T : class
    {
        // 需要用 _options 來建立 DbSet
        return new DbSet<T>(_options);
        //                  ^^^^^^^^ 
        //                  options 在這裡被使用
    }
}
```

**結論：options 是 DbContext 的「生命線」，沒有它就無法運作**

### 8.4 三個角色的對話

```
場景：建立 MyBoardsContext

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

DI 容器：
「我要建立 MyBoardsContext」
「我有 DbContextOptions（從 AddDbContext 來的）」
「但是... 我要怎麼把 options 傳給 MyBoardsContext？」

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

❌ 如果沒有建構函式：

MyBoardsContext：
「我沒有建構函式，你只能 new MyBoardsContext()」

DI 容器：
「可是我有 options 要給你啊！」

DbContext（父類別）：
「等等！我需要 options 才能運作！」
「沒有 options 我無法連接資料庫！」

C# 編譯器：
「❌ 停！這樣不符合規則！」
「DbContext 需要 options，但沒有辦法傳入」
「編譯失敗！」

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ 如果有建構函式：

MyBoardsContext：
「我有建構函式：MyBoardsContext(DbContextOptions options)」

DI 容器：
「太好了！我知道怎麼建立你了」
「var context = new MyBoardsContext(options);」
「搞定！」

MyBoardsContext 建構函式執行：
「收到 options」
「透過 : base(options) 傳給父類別」

DbContext：
「收到 options！」
「開始初始化資料庫連接...」
「完成！可以使用了！」

✅ 成功！
```

---

## 9. 常見問題 Q&A

### Q1: 為什麼要用 `base(options)` 而不是直接在建構函式裡處理？

**答：** 因為資料庫連接的邏輯都在父類別 `DbContext` 裡。

```csharp
public class MyBoardsContext : DbContext
{
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
        : base(options)  // ← 必須這樣寫
    {
        // 如果不用 : base(options)，父類別不會被初始化
        // DbContext 內部的連接管理、查詢編譯器等都無法建立
    }
}
```

### Q2: 建構函式的本體為什麼是空的？

**答：** 因為所有的初始化工作都在 `: base(options)` 完成了。

```csharp
public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
    : base(options)  // 這裡已經做完所有初始化
{
    // 通常是空的
    // 除非你有特殊需求，例如：
    // - 設定預設值
    // - 註冊事件
    // - 建立額外的物件
}
```

### Q3: 可以有多個建構函式嗎？

**答：** 可以，但至少要有一個接受 `DbContextOptions` 的。

```csharp
public class MyBoardsContext : DbContext
{
    // 給 DI 容器用的建構函式
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
        : base(options)
    {
    }
    
    // 給測試或特殊情況用的建構函式
    public MyBoardsContext(
        DbContextOptions<MyBoardsContext> options,
        ILogger<MyBoardsContext> logger
    )
        : base(options)
    {
        _logger = logger;
    }
}
```

### Q4: Lambda 中的 `options` 和建構函式的 `options` 是同一個東西嗎？

**答：** 不是，但有關聯。

```csharp
// Program.cs 中的 lambda
builder.Services.AddDbContext<MyBoardsContext>(options =>
//                                             ^^^^^^^ 
//                                             這是 DbContextOptionsBuilder
{
    options.UseSqlServer("...");
});

// MyBoardsContext 建構函式
public MyBoardsContext(DbContextOptions<MyBoardsContext> options)
//                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                     這是 DbContextOptions（注意沒有 Builder）
    : base(options)
{
}

// 關係：
// DbContextOptionsBuilder (Lambda 中) 
//     ↓ 呼叫 .Options
// DbContextOptions (建構函式參數)
```

### Q5: 為什麼要用 Lambda 而不是直接寫？

**答：** 因為 Lambda 提供了延遲執行和靈活配置的能力。

```csharp
// 使用 Lambda（推薦）
builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    // 可以使用條件判斷
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlServer("開發資料庫");
        options.EnableSensitiveDataLogging();
    }
    else
    {
        options.UseSqlServer("正式資料庫");
    }
});

// 如果不用 Lambda，你需要在每個地方都寫這些判斷
```

### Q6: 如果我的 DbContext 需要其他服務怎麼辦？

**答：** 可以在建構函式中注入其他服務。

```csharp
public class MyBoardsContext : DbContext
{
    private readonly ILogger<MyBoardsContext> _logger;
    
    public MyBoardsContext(
        DbContextOptions<MyBoardsContext> options,
        ILogger<MyBoardsContext> logger  // 注入其他服務
    )
        : base(options)
    {
        _logger = logger;
    }
    
    public override int SaveChanges()
    {
        _logger.LogInformation("正在儲存變更...");
        return base.SaveChanges();
    }
}
```

### Q7: 為什麼是 Scoped 生命週期？

**答：** 因為 DbContext 應該在一個請求內共享，但不同請求要用不同的實例。

```csharp
Scoped（預設）：
├─ 每個 HTTP 請求建立一個新的 DbContext
├─ 同一個請求內多次注入會拿到同一個實例
└─ 請求結束後自動 Dispose

為什麼不用 Singleton？
└─ DbContext 不是執行緒安全的
   多個請求同時使用同一個 DbContext 會出錯

為什麼不用 Transient？
└─ 同一個請求內可能需要多次使用 DbContext
   使用 Transient 會建立多個實例，造成資料不一致
```

### Q8: 可以不用 DI 嗎？

**答：** 可以，但非常不推薦。

```csharp
// 不用 DI（不推薦）
public class WorkItemsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var options = new DbContextOptionsBuilder<MyBoardsContext>()
            .UseSqlServer("連接字串")
            .Options;
            
        using var context = new MyBoardsContext(options);
        return Ok(context.WorkItems.ToList());
    }
}

// 問題：
// ❌ 每次都要手動建立 options
// ❌ 連接字串寫死在程式碼裡
// ❌ 難以測試
// ❌ 要記得 Dispose
// ❌ 無法集中管理設定
```

---

## 完整範例程式碼

### Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;

var builder = WebApplication.CreateBuilder(args);

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 註冊 DbContext 到 DI 容器
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

builder.Services.AddDbContext<MyBoardsContext>(options =>
{
    // 從設定檔讀取連接字串
    var connectionString = builder.Configuration
        .GetConnectionString("MyBoardsDb");
    
    // 設定使用 SQL Server
    options.UseSqlServer(connectionString);
    
    // 開發環境額外設定
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();  // 顯示參數值
        options.LogTo(Console.WriteLine);       // 輸出 SQL 到 console
    }
});

// 註冊 Controllers
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

### MyBoardsContext.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace MyBoards.Entities;

public class MyBoardsContext : DbContext
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 建構函式：接收 DI 容器傳來的 options
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public MyBoardsContext(DbContextOptions<MyBoardsContext> options) 
        : base(options)  // 傳給父類別 DbContext
    {
        // 建構函式本體通常是空的
        // 因為所有初始化都在 base(options) 完成了
    }

    // DbSet 屬性用於查詢和操作資料表
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Address> Addresses { get; set; }
    
    // 可選：覆寫 OnModelCreating 來配置模型
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置實體關係、限制等
        modelBuilder.Entity<WorkItem>()
            .HasKey(w => w.Id);
    }
}
```

### WorkItemsController.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBoards.Entities;

namespace MyBoards.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkItemsController : ControllerBase
{
    private readonly MyBoardsContext _context;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 建構函式注入：要求 DI 容器提供 MyBoardsContext
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public WorkItemsController(MyBoardsContext context)
    {
        _context = context;  // DI 容器會自動傳入已配置好的 context
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // 直接使用 _context，不用擔心：
        // ✅ 資料庫如何連接
        // ✅ 連接字串在哪裡
        // ✅ 何時建立、何時釋放
        // ✅ 所有這些都由 DI 容器管理
        
        var workItems = await _context.WorkItems.ToListAsync();
        return Ok(workItems);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var workItem = await _context.WorkItems.FindAsync(id);
        
        if (workItem == null)
            return NotFound();
            
        return Ok(workItem);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(WorkItem workItem)
    {
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(
            nameof(GetById),
            new { id = workItem.Id },
            workItem
        );
    }
}
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "MyBoardsDb": "Server=localhost;Database=MyBoards;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## 總結

### 核心概念

1. **建構函式是物件初始化的入口**
   - 可以要求必要的參數
   - 繼承時需要呼叫父類別的建構函式

2. **DI 容器是智慧工廠**
   - 自動管理物件的建立和生命週期
   - 根據建構函式自動注入依賴

3. **Lambda 是匿名函式**
   - 簡潔地定義行為
   - 延遲執行，提供靈活性

4. **DbContext 需要 DbContextOptions**
   - 包含資料庫連接資訊
   - 透過建構函式傳入

5. **AddDbContext 串連所有元素**
   - 註冊服務到 DI 容器
   - 配置 DbContextOptions
   - 自動建立 DbContext 實例

### 必須記住的規則

| 元件 | 必須做的事 | 原因 |
|------|-----------|------|
| **MyBoardsContext** | 必須有接受 `DbContextOptions` 的建構函式 | 父類別 `DbContext` 需要 options 才能初始化 |
| **建構函式** | 必須呼叫 `: base(options)` | 將 options 傳給父類別 |
| **Program.cs** | 必須呼叫 `AddDbContext` 註冊服務 | DI 容器需要知道如何建立 DbContext |
| **Lambda** | 必須配置至少一個資料庫提供者 | DbContext 需要知道要連哪個資料庫 |
| **Controller** | 透過建構函式接收 DbContext | 讓 DI 容器自動注入 |

### 檢查清單

開發 EF Core 應用程式時，確保：

- [ ] `MyBoardsContext` 有建構函式接受 `DbContextOptions<MyBoardsContext>`
- [ ] 建構函式使用 `: base(options)` 呼叫父類別
- [ ] `Program.cs` 中呼叫 `AddDbContext` 註冊服務
- [ ] Lambda 中配置了資料庫提供者（如 `UseSqlServer`）
- [ ] 連接字串設定在 `appsettings.json` 中
- [ ] Controller 透過建構函式注入接收 DbContext
- [ ] 不要手動 `new` DbContext（讓 DI 容器處理）
- [ ] 不要手動 `Dispose` DbContext（DI 容器會自動清理）

---

## 延伸閱讀

- [Microsoft Docs - Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Microsoft Docs - Dependency Injection](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [Microsoft Docs - DbContext Lifetime](https://docs.microsoft.com/ef/core/dbcontext-configuration/)
- [Lambda 表達式完整指南](https://docs.microsoft.com/dotnet/csharp/language-reference/operators/lambda-expressions)

---

**文件建立日期：** 2026年3月12日  
**適用版本：** .NET 6.0+, Entity Framework Core 6.0+
