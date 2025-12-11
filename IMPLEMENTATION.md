# 影片下載功能實作說明

## 功能概述

本專案實作了完整的影片下載功能，可以從網頁自動提取影片連結，並下載轉換為 MP4 格式。

## 實作細節

### 1. 網頁解析 (VideoExtractorService.cs)

使用 **PuppeteerSharp 20.2.5** 實現無頭瀏覽器功能：

- 自動下載 Chromium 瀏覽器（首次執行）
- 載入目標網頁並等待完全渲染
- 監聽網路請求，攔截影片相關的 URL（m3u8、mp4、ts 等）
- 注入 JavaScript 攔截 XMLHttpRequest、fetch、MediaSource 等 API
- 從 `<video>` 和 `<source>` 標籤提取 src 屬性
- 處理 iframe 嵌入的影片
- 支援 Blob URL 的來源追蹤
- 自動嘗試觸發影片播放以載入資源

### 2. 影片下載 (DownloadService.cs)

支援多種下載模式：

#### M3U8/HLS 串流
- 解析 M3U8 播放清單
- 並行下載所有 TS 片段（預設 10 個並行連線）
- 支援 AES-128-CBC 加密解密
- 使用 FFMpegCore 合併片段為 MP4

#### 直接檔案下載
- 檢測伺服器是否支援 Range 請求
- 支援分段並行下載
- 串流下載模式（不支援 Range）
- 即時顯示下載進度、速度、預估剩餘時間

#### 自動轉檔
- 下載非 MP4 格式時自動轉換
- 使用 FFMpegCore 進行轉碼
- 轉換完成後可選擇刪除原始檔案

### 3. FFmpeg 處理 (FFmpegService.cs)

使用 **FFMpegCore 5.1.0** 套件：

- 自動檢測系統 FFmpeg 安裝
- 提供 FFmpeg 自動安裝功能（Windows/Linux/macOS）
- 使用 FFMpegCore API 進行影片合併和轉碼
- 失敗時自動回退到直接呼叫 FFmpeg 命令列
- 支援自訂 FFmpeg 二進位檔路徑

### 4. 下載目錄

所有影片統一下載到 `影片/` 資料夾：

```csharp
string videoDir = Path.Combine(Directory.GetCurrentDirectory(), "影片");
Directory.CreateDirectory(videoDir); // 自動建立資料夾
```

### 5. 錯誤處理

完整的錯誤處理機制：

- 網頁載入失敗處理
- 找不到 video 標籤時的提示
- 下載失敗時的重試機制
- FFmpeg 轉換失敗的回退方案
- 適當的錯誤訊息顯示

### 6. 進度顯示

即時進度資訊：

```
下載進度: 45.2 MB/100.0 MB (45.2%) | 速度: 2.3 MB/s | 剩餘: 24 秒
```

## 使用流程

1. 啟動程式
2. 選擇輸入模式：
   - 模式 1：直接輸入影片 URL
   - 模式 2：輸入網頁 URL，自動提取
3. 輸入檔案名稱
4. 自動下載並轉換為 MP4
5. 影片儲存在 `影片/` 資料夾

## 技術要點

### PuppeteerSharp 配置

```csharp
await Puppeteer.LaunchAsync(new LaunchOptions
{
    Headless = true,
    Args = new[]
    {
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-dev-shm-usage",
        "--disable-web-security",
        "--autoplay-policy=no-user-gesture-required"
    }
});
```

### FFMpegCore 使用

```csharp
// 合併影片片段
await FFMpegArguments
    .FromConcatInput(segmentFiles, options => options
        .WithCustomArgument("-safe 0"))
    .OutputToFile(outputFile, true, options => options
        .CopyChannel())
    .ProcessAsynchronously();

// 轉換影片格式
await FFMpegArguments
    .FromFileInput(inputFile)
    .OutputToFile(outputFile, true, options => options
        .WithVideoCodec("libx264")
        .WithAudioCodec("aac")
        .WithFastStart())
    .ProcessAsynchronously();
```

## 相依套件

- **PuppeteerSharp 20.2.5**：無頭瀏覽器操作
- **FFMpegCore 5.1.0**：FFmpeg .NET 封裝

## 未來改進建議

1. 支援更多影片網站的特殊解析規則
2. 新增下載佇列功能
3. 支援斷點續傳
4. 新增影片品質選擇功能
5. 提供 GUI 介面選項
