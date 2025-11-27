# webupdater

可透過 API 進行網站更新的 .NET 專案，支援自動備份、應用程式集區管理及系統監控功能。

## 功能特色

- 🔄 **自動更新網站程式**：透過 API 上傳 ZIP 檔案自動更新網站
- 💾 **自動備份功能**：更新前自動備份，支援時間戳記備份目錄
- 🗑️ **自動清理舊備份**：自動清理超過 7 天的備份檔案，節省磁碟空間
- 🎛️ **應用程式集區管理**：啟動、停止應用程式集區
- 📊 **系統監控**：取得 CPU 及記憶體使用率
- 🔐 **HMAC 認證**：安全的 API 金鑰認證機制

## 系統需求

- .NET 8.0 或更高版本
- Windows Server 與 IIS
- 管理員權限（用於管理 IIS 應用程式集區）

## 安裝說明

### 1. 建立應用程式集區

1. 開啟 IIS 管理員
2. 建立新的應用程式集區
3. **重要**：將應用程式集區的權限設為 **LocalSystem**（控制應用程式集區需要此權限）

### 2. 建立 IIS 站台

1. 在 IIS 中建立新的網站
2. 將應用程式集區指向步驟 1 建立的應用程式集區
3. 將編譯後的程式檔案部署到 IIS 站台目錄

### 3. 設定應用程式設定檔

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
    ]
  }
}
```

**設定說明：**
- `AppName`：IIS 應用程式集區的名稱（必須與 IIS 中的名稱完全一致）
- `DirectoryName`：網站檔案所在的完整路徑
- `BackupPath`：備份檔案的基礎路徑（更新前會自動在此路徑下建立帶時間戳記的備份目錄）

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

#### 4. 取得電腦資訊

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

1. **權限設定**：應用程式集區必須設定為 LocalSystem 權限，否則無法控制應用程式集區
2. **備份路徑**：請確保備份路徑有足夠的磁碟空間（系統會自動清理超過 7 天的備份）
3. **檔案格式**：更新檔案必須為 ZIP 格式
4. **API 金鑰**：每個 API 端點都有不同的原始金鑰，需使用對應的 API Key
5. **金鑰有效期**：API 金鑰僅在當天有效，隔天需重新產生
6. **備份保留期限**：備份檔案會自動保留 7 天，超過 7 天的備份會在下次更新時自動刪除

## 授權

詳見 [LICENSE](LICENSE) 檔案。
