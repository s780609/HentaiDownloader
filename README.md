# HentaiDownloader

一個使用 .NET 10.0 開發的高效能影片下載器，支援 M3U8 串流、MP4、TS 等多種格式。

## ✨ 功能特色

- 🎬 **多格式支援** - 支援 M3U8 (HLS)、MP4、TS 等影片格式
- 🌐 **網頁提取** - 使用 PuppeteerSharp 自動從網頁提取影片連結
- ⚡ **並行下載** - 使用多執行緒並行下載，大幅提升下載速度 (預設 10 個並行連線)
- 🔐 **AES-128 解密** - 自動處理加密的 M3U8 串流 (支援 AES-128-CBC)
- 📊 **即時進度顯示** - 顯示下載進度、速度、剩餘時間
- 🌐 **Unicode 支援** - 完整支援中文、日文等多國語言檔名
- 🔄 **分段續傳** - 支援 HTTP Range 請求，適合大檔案下載
- 🎞️ **自動轉檔** - 使用 FFMpegCore 自動合併片段並轉換為 MP4

## 系統需求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) 或更高版本
- [FFmpeg](https://ffmpeg.org/download.html) (M3U8 下載和影片轉檔需要，需加入 PATH 環境變數)

## 快速開始

### 安裝

```bash
# 複製專案
git clone https://github.com/s780609/HentaiDownloader.git
cd HentaiDownloader

# 建置專案
dotnet build

# 執行程式
dotnet run --project HentaiDownloader
```

### 使用方式

1. 執行程式
2. 輸入影片 URL (支援 M3U8 或直接連結)
3. 輸入輸出檔案名稱 (支援中文/日文/空格)
4. 等待下載完成

```
=== 影片下載器 / ビデオダウンローダー ===
支援 / サポート: M3U8, MP4, TS 等格式

請輸入影片 URL: https://example.com/video/playlist.m3u8
請輸入輸出檔案名稱 (不含副檔名，支援中文/日文/空格): 我的影片

偵測到 M3U8 連結...
正在下載 M3U8...
找到 150 個片段
使用 10 個並行下載
下載進度: 150/150 (100.0%) | 速度: 8.5 片段/秒 | 剩餘: 0 秒
下載完成! 耗時: 17.6 秒
正在合併片段並轉換為 MP4...
✅ 下載完成: C:\Downloads\我的影片.mp4
```

## 支援的下載類型

| 類型 | 說明 | 需要 FFmpeg |
|------|------|-------------|
| M3U8 (HLS) | HTTP Live Streaming 串流 | ✅ 是 |
| MP4 | 直接下載 MP4 檔案 | ❌ 否 |
| TS | 直接下載 TS 檔案 | ❌ 否 |
| 其他 | 其他直接連結格式 | ❌ 否 |

## 專案結構

```
HentaiDownloader/
├── HentaiDownloader.sln          # Visual Studio 解決方案檔案
├── README.md                     # 說明文件
└── HentaiDownloader/
    ├── HentaiDownloader.csproj   # 專案檔案
    ├── Program.cs                # 主程式
    ├── Services/                 # 服務類別
    │   ├── ConsoleService.cs     # 控制台服務
    │   ├── DownloadService.cs    # 下載服務
    │   ├── FFmpegService.cs      # FFmpeg 服務
    │   ├── UserInputService.cs   # 使用者輸入服務
    │   └── VideoExtractorService.cs  # 影片提取服務
    ├── Models/                   # 資料模型
    │   └── M3U8Info.cs           # M3U8 資訊模型
    ├── 影片/                      # 下載的影片儲存目錄
    ├── bin/                      # 編譯輸出
    └── obj/                      # 中間檔案
```

## 技術細節

### 下載策略

- **網頁提取**: 使用 PuppeteerSharp 無頭瀏覽器載入網頁，自動提取 video 標籤和網路請求中的影片 URL
- **M3U8 串流**: 解析 M3U8 播放清單，並行下載所有 TS 片段，最後使用 FFMpegCore 合併
- **直接連結**: 檢查伺服器是否支援 Range 請求，支援則使用分段並行下載，否則使用串流下載
- **自動轉檔**: 下載非 MP4 格式時，自動使用 FFMpegCore 轉換為 MP4

### 加密處理

支援 AES-128-CBC 加密的 M3U8 串流：
- 自動從 `#EXT-X-KEY` 標籤取得金鑰 URL
- 支援自訂 IV 或使用預設 IV (片段序號)
- 下載後自動解密每個片段

### 使用的技術與套件

- **PuppeteerSharp 20.2.5**: 無頭瀏覽器，用於網頁解析和影片 URL 提取
- **FFMpegCore 5.1.0**: FFmpeg 的 .NET 封裝，用於影片合併和轉檔

## 開發環境設定

1. 安裝 [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
2. 安裝 [FFmpeg](https://ffmpeg.org/download.html) 並加入 PATH
3. 使用 Visual Studio 2022 或 VS Code 開啟 `HentaiDownloader.sln`
4. 還原 NuGet 套件：`dotnet restore`
5. 建置專案：`dotnet build`

## 授權

此專案尚未指定授權。

## 貢獻

歡迎提交 Issue 和 Pull Request！
