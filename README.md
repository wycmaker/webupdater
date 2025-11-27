# webupdater

可透過 API 進行網站更新的 .NET 專案，支援自動備份、應用程式集區管理及系統監控功能。

## 功能特色

- 🔄 **自動更新網站程式**：透過 API 上傳 ZIP 檔案自動更新網站
- ⚛️ **NextJS 專案更新**：支援 NextJS 專案自動更新，整合 PM2 管理
- 💾 **自動備份功能**：更新前自動備份，支援時間戳記備份目錄
- 🗑️ **自動清理舊備份**：自動清理超過 7 天的備份檔案，節省磁碟空間
- 🎛️ **應用程式集區管理**：啟動、停止應用程式集區
- 📊 **系統監控**：取得 CPU 及記憶體使用率
- 🔐 **HMAC 認證**：安全的 API 金鑰認證機制

## 系統需求

- .NET 8.0 或更高版本
- Windows Server 與 IIS（用於 IIS 應用程式集區管理）
- PM2（用於 NextJS 專案管理，需全域安裝）
- 系統管理員權限（用於管理 IIS 應用程式集區及執行 PM2 命令）

## 安裝說明

### 1. 建立應用程式集區

1. 開啟 IIS 管理員
2. 建立新的應用程式集區
3. **重要**：將應用程式集區的權限設為 **LocalSystem**（控制應用程式集區需要此權限）

### 2. 建立 IIS 站台

1. 在 IIS 中建立新的網站
2. 將應用程式集區指向步驟 1 建立的應用程式集區
3. 將編譯後的程式檔案部署到 IIS 站台目錄

### 3. 設定 PM2（僅 NextJS 專案需要）

如果您的專案包含 NextJS 專案更新功能，需要設定 PM2 以便所有使用者都能存取：

#### 設定 npm 全域安裝路徑到系統目錄（推薦）

此方案可讓 PM2 在所有使用者環境下都能正常運作，特別適合 IIS 的 LocalSystem 環境。

**步驟 1：建立系統全域 npm 目錄**

以系統管理員權限開啟命令提示字元，執行：

```cmd
mkdir C:\npm
mkdir C:\npm\node_modules
```

**步驟 2：設定 npm 的全域安裝路徑**

```cmd
npm config set prefix "C:\npm"
```

**步驟 3：將路徑加入系統 PATH 環境變數**

1. 按 `Win + R`，輸入 `sysdm.cpl`，按 Enter
2. 點擊「進階」標籤 → 「環境變數」
3. 在「系統變數」區塊找到 `Path`，點擊「編輯」
4. 點擊「新增」，加入：`C:\npm`
5. 點擊「確定」儲存所有變更

**步驟 4：重新安裝 PM2 到新位置**

```cmd
npm install -g pm2
```

**步驟 5：驗證安裝**

```cmd
pm2 --version
```

如果顯示版本號，表示安裝成功。

**步驟 6：重啟 IIS 應用程式集區**

設定完成後，必須重啟 IIS 應用程式集區才能讓環境變數生效：
1. 開啟 IIS 管理員
2. 選取您的應用程式集區
3. 點擊「回收」或「重新啟動」

**優點：**
- ✅ 所有使用者都能存取
- ✅ 符合 npm 的標準做法
- ✅ 未來安裝其他全域套件也會安裝到系統目錄
- ✅ 不需要手動複製檔案

### 4. 設定應用程式設定檔

編輯 `appsettings.json`，設定應用程式資訊：

```json
{
  "AppInfo": {
    "AppSetting": [
      {
        "AppName": "應用程式集區名稱",
        "DirectoryName": "網站檔案路徑",
        "BackupPath": "備份檔案路徑"
      }
    ],
    "NextJSProjects": [
      {
        "ProjectName": "專案名稱",
        "Pm2Id": "PM2 程序 ID",
        "DirectoryPath": "網站檔案路徑",
        "BackupPath": "備份檔案路徑"
      }
    ]
  }
}
```

**設定說明：**

>  AppSetting

- `AppName`：IIS 應用程式集區的名稱（必須與 IIS 中的名稱完全一致）
- `DirectoryName`：網站檔案所在的完整路徑
- `BackupPath`：備份檔案的基礎路徑（更新前會自動在此路徑下建立帶時間戳記的備份目錄）
> NextJSProjects

- `ProjectName`：專案名稱（用於 API 端點識別）
- `Pm2Id`：PM2 程序 ID（可透過 `pm2 list` 查詢）
- `DirectoryPath`：NextJS 專案所在的完整路徑
- `BackupPath`：備份檔案的基礎路徑（更新前會自動在此路徑下建立帶時間戳記的備份目錄，**備份時會排除 node_modules**）

**範例：**
```json
{
  "AppInfo": {
    "AppSetting": [
      {
        "AppName": "newTalent",
        "DirectoryName": "C:\\WebSite\\newTalent",
        "BackupPath": "C:\\Backup\\newTalent"
      }
    ],
    "NextJSProjects": [
      {
        "ProjectName": "myNextJSApp",
        "Pm2Id": "0",
        "DirectoryPath": "C:\\Projects\\myNextJSApp",
        "BackupPath": "C:\\Projects\\Backup\\myNextJSApp"
      }
    ]
  }
}
```

## 使用說明

### API 金鑰產生

1. 執行 `KeyGenerate.exe` 產生 API 金鑰
2. **重要**：每個 API 端點都有不同的原始金鑰（Secret），需產生對應的 API Key 才能使用
3. **金鑰有效期**：產生的金鑰有效期為當天（基於日期驗證）

### API 認證

所有 API 請求都需要在 HTTP Header 中包含以下資訊：

- `X-API-KEY`：指定API使用的 API key
- `X-Signature`：HMAC 簽名（由 KeyGenerate.exe 產生）

### API 端點

#### 1. 更新網站程式

**端點：** `POST /api/update/pool/{appPoolName}`

**說明：** 更新指定應用程式集區的網站程式，更新前會自動停止應用程式集區、執行備份，更新完成後自動啟動應用程式集區。

**參數：**
- `appPoolName`（路徑參數）：應用程式集區名稱
- `file`（表單資料）：ZIP 格式的更新檔案

**請求範例：**
```bash
curl -X POST "https://your-server/api/update/pool/newTalent" \
  -H "X-API-KEY: your-api-key" \
  -H "X-Signature: your-signature" \
  -F "file=@update.zip"
```

**回應範例：**
```json
{
  "message": "newTalent的程式已成功更新"
}
```

**更新流程：**

當執行更新 API 時，系統會依序執行以下步驟：

1. ✅ 驗證應用程式集區是否存在於設定檔中
2. ✅ 驗證上傳檔案格式（必須為 ZIP）
3. ✅ 停止應用程式集區
4. ✅ 執行備份（如果設定了備份路徑）
5. ✅ 清理超過 7 天的舊備份檔案
6. ✅ 解壓縮並更新檔案
7. ✅ 啟動應用程式集區
8. ✅ 回傳更新結果

**備份說明：**
- 如果設定了 `BackupPath`，系統會在更新前自動備份整個目錄
- 備份目錄格式：`{BackupPath}/{AppName}_{yyyyMMdd_HHmmss}`
- 例如：`C:\Backup\newTalent\newTalent_20240101_143022`
- **自動清理**：系統會在每次備份後自動清理超過 7 天的舊備份檔案，避免占用過多磁碟空間

#### 2. 啟動應用程式集區

**端點：** `POST /api/update/pool/{appPoolName}/start`

**說明：** 啟動指定的應用程式集區。

**參數：**
- `appPoolName`（路徑參數）：應用程式集區名稱

**回應範例：**
```json
{
  "message": "應用程式集區成功啟用（狀態為：Started）"
}
```

#### 3. 停止應用程式集區

**端點：** `POST /api/update/pool/{appPoolName}/stop`

**說明：** 停止指定的應用程式集區。

**參數：**
- `appPoolName`（路徑參數）：應用程式集區名稱

**回應範例：**
```json
{
  "message": "應用程式集區成功停用（狀態為：Stopped）"
}
```

#### 4. 更新 NextJS 專案

**端點：** `POST /api/update/nextjs/{projectName}`

**說明：** 更新指定 NextJS 專案，更新前會自動停止 PM2 程序、執行備份（排除 node_modules）、清除舊檔案，更新完成後自動啟動 PM2 程序並儲存設定。

**參數：**
- `projectName`（路徑參數）：NextJS 專案名稱（需在設定檔中定義）
- `file`（表單資料）：ZIP 格式的更新檔案

**請求範例：**
```bash
curl -X POST "https://your-server/api/update/nextjs/myNextJSApp" \
  -H "X-API-KEY: your-api-key" \
  -H "X-Signature: your-signature" \
  -F "file=@update.zip"
```

**回應範例：**
```json
{
  "message": "myNextJSApp 專案已成功更新",
  "details": {
    "pm2Id": "0",
    "directoryPath": "C:\\Projects\\myNextJSApp",
    "backupPath": "C:\\Projects\\Backup\\myNextJSApp"
  }
}
```

**更新流程：**

當執行 NextJS 更新 API 時，系統會依序執行以下步驟：

1. ✅ 驗證專案是否存在於設定檔中
2. ✅ 驗證上傳檔案格式（必須為 ZIP）
3. ✅ 停止 PM2 程序（`pm2 stop {id}`）
4. ✅ 執行備份（如果設定了備份路徑，**排除 node_modules**）
5. ✅ 清理超過 7 天的舊備份檔案
6. ✅ 清除專案目錄中的舊檔案
7. ✅ 解壓縮並更新檔案
8. ✅ 啟動 PM2 程序（`pm2 start {id}`）
9. ✅ 儲存 PM2 設定（`pm2 save`）
10. ✅ 回傳更新結果

**備份說明：**
- 如果設定了 `BackupPath`，系統會在更新前自動備份整個目錄
- **備份時會自動排除 `node_modules` 目錄**，節省備份空間和時間
- 備份目錄格式：`{BackupPath}/{ProjectName}_{yyyyMMdd_HHmmss}`
- 例如：`C:\Projects\Backup\myNextJSApp\myNextJSApp_20240101_143022`
- **自動清理**：系統會在每次備份後自動清理超過 7 天的舊備份檔案

#### 5. 取得電腦資訊

**端點：** `POST /api/update/computer/info`

**說明：** 取得伺服器的 CPU 使用率及記憶體資訊。

**回應範例：**
```json
{
  "cpuUsage": 25.5,
  "usedMemory": 8192.0,
  "avaliableMemory": 16384.0
}
```

## 注意事項

### IIS 應用程式集區相關

1. **權限設定**：應用程式集區必須設定為 LocalSystem 權限，否則無法控制應用程式集區
2. **備份路徑**：請確保備份路徑有足夠的磁碟空間（系統會自動清理超過 7 天的備份）

### NextJS 專案相關

3. **PM2 安裝**：必須全域安裝 PM2（`npm install -g pm2`），**建議使用系統目錄安裝**（如 `C:\npm`），以便在 LocalSystem 環境下正常運作。詳細設定步驟請參考「安裝說明」中的「設定 PM2」章節
4. **PM2 程序 ID**：確保 `Pm2Id` 設定正確，可透過 `pm2 list` 命令查詢
5. **node_modules 處理**：備份時會自動排除 `node_modules`，更新時會清除專案目錄但保留 `node_modules`（如果存在）

### 通用注意事項

6. **檔案格式**：更新檔案必須為 ZIP 格式
7. **API 金鑰**：每個 API 端點都有不同的原始金鑰，需使用對應的 API Key
8. **金鑰有效期**：API 金鑰僅在當天有效，隔天需重新產生
9. **備份保留期限**：備份檔案會自動保留 7 天，超過 7 天的備份會在下次更新時自動刪除
10. **系統管理員權限**：應用程式必須以系統管理員權限執行，才能正確執行 PM2 命令
11. **請求體大小限制**：預設支援最大 500MB 的檔案上傳，如需調整請修改 `Program.cs` 和 `web.config` 中的設定

## 授權

詳見 [LICENSE](LICENSE) 檔案。
