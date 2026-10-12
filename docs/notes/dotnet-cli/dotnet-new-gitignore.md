# 用 `dotnet new gitignore` 產生 .gitignore

- 標籤：#dotnet-cli #git #gitignore
- 最後更新：2026-10-12

## 重點摘要

- 在專案根目錄（`.sln` 所在處）執行：`dotnet new gitignore`
- 產生的是 GitHub 官方的 [VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore) 模板，已涵蓋 `bin/`、`obj/`、`.vs/`、Rider 的 `.idea/` 等。
- 已有 `.gitignore` 時不會覆蓋，要加 `--force`。
- 檔案已經被 git 追蹤的話，加進 .gitignore 也沒用，要先 `git rm --cached`。

## 詳細說明

### 基本用法

```bash
# 在 repo 根目錄執行（跟 .sln 同一層）
dotnet new gitignore

# 已存在 .gitignore 時強制覆蓋
dotnet new gitignore --force

# 指定輸出位置
dotnet new gitignore -o <資料夾>
```

- 從 .NET Core 3.0 SDK 起內建這個模板，不用另外安裝。
- 可用 `dotnet new list gitignore`（.NET 6 也可用 `dotnet new --list`）確認模板存在。

### 放在哪一層

`.gitignore` 規則會套用到它所在的資料夾及所有子資料夾，所以放在 **repo 根目錄** 就能涵蓋所有專案。
本專案位置：`.gitignore`（與 `MyBoards.sln` 同層）。

### 已追蹤的檔案要先移出索引

例如 `bin/`、`obj/` 在加 .gitignore 前就被 commit 了：

```bash
git rm -r --cached MyBoards/bin MyBoards/obj
git commit -m "chore: 移除不該追蹤的建置輸出"
```

`--cached` 只從 git 索引移除，不會刪掉本機檔案。

### 確認某個檔案為什麼被忽略

```bash
git check-ignore -v MyBoards/obj/project.assets.json
# 會顯示是 .gitignore 的第幾行規則命中
```

## 範例

本專案的 `.gitignore` 就是用這個指令產生的（commit `be9eb89`），約 450 行，
Rider 相關規則在第 443 行附近：

```gitignore
# JetBrains Rider
.idea/
*.sln.iml
```

## 參考資料

- [dotnet new 內建模板（Microsoft Learn）](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates)
- [github/gitignore - VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore)
