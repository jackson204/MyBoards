# MyBoards 開發筆記

開發過程中遇到的問題與學習筆記，方便日後查找。

## 怎麼寫

| 類型 | 位置 | 說明 | 模板 |
|------|------|------|------|
| 問題紀錄 | `problems/` | 一次性踩坑：問題 → 知識補充 → 解法。檔名 `YYYY-MM-DD-簡短描述.md` | [problem.md](_templates/problem.md) |
| 學習筆記 | `notes/<主題>/` | 主題式知識，會持續補充 | [note.md](_templates/note.md) |

- 問題紀錄中有通用價值的知識，整理到 `notes/` 並互相連結。
- 新增筆記後，在下方索引加一行：`- [標題](路徑) — #tag 一句話摘要`
- 查找：錯誤訊息原文或標籤，用 Rider「Find in Files」或 `git grep "<關鍵字>" -- docs/`

## 索引

### 問題紀錄

- [README.md 是 UTF-16 編碼，工具讀出亂碼](problems/2026-10-12-readme-utf16-encoding.md) — #encoding #git 用 PowerShell 5 重導向建檔導致 UTF-16

### 學習筆記

- [用 `dotnet new gitignore` 產生 .gitignore](notes/dotnet-cli/dotnet-new-gitignore.md) — #dotnet-cli #git 指令用法、`--force`、已追蹤檔案用 `git rm --cached`
- [EF Core 連接資料庫（Code First）](notes/ef-core/connect-database.md) — #ef-core #migration 原理、套件、主鍵與關聯、DbContext、Migration、常見錯誤
