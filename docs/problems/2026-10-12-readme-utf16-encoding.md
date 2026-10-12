# README.md 是 UTF-16 編碼，工具讀出亂碼

- 日期：2026-10-12
- 標籤：#encoding #git #windows
- 相關 commit：（本次 docs 初始化 commit）

## 問題

根目錄 `README.md` 用部分工具讀取時出現亂碼，字元之間夾著空白：

```
��#   M y B o a r d s
```

## 原因 / 知識補充

- 檔案被存成 **UTF-16 LE（含 BOM `FF FE`）**，每個 ASCII 字元佔 2 bytes，第二個 byte 是 `00`，所以用 UTF-8 讀會變成「字元 + 空白」，開頭的 BOM 顯示成 `��`。
- 常見來源：Windows PowerShell 5.x 的 `echo "..." > README.md` / `Out-File` 預設輸出 UTF-16 LE。
  PowerShell 7+ 預設已改為 UTF-8（無 BOM）。
- git 會把 UTF-16 檔案當成 binary，`git diff` 看不到內容差異；GitHub 顯示也可能異常。

## 解決方式

把檔案改存成 UTF-8：

- Rider：右下角狀態列的編碼 → 選 `UTF-8` → Convert
- 或 PowerShell 7：`Get-Content README.md | Set-Content README.md -Encoding utf8`

## 學到什麼 / 下次怎麼避免

- 在 Windows PowerShell 5.x 用重導向建立文字檔時，指定 `-Encoding utf8`，或改用 PowerShell 7。
- 可加 `.editorconfig` 設定 `charset = utf-8` 統一編碼。
