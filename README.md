# HentaiDownloader

一個包含兩個應用程式的 .NET 解決方案：

- **HentaiDownloader**（.NET 10.0 主控台）— 高效能影片下載器，支援 M3U8 串流、MP4、TS 等多種格式，並可批量下載 Hanime1 裏番。
- **VideoPlayer**（WinUI 3 桌面程式，Windows 限定）— 瀏覽與播放本機影片的播放器，支援縮圖、資料夾導航與記憶播放位置。

## ✨ 功能特色

### HentaiDownloader（下載器）

- 🧭 **雙下載模式** - 啟動時可選擇「手動輸入 URL 下載」或「批量下載 Hanime1 裏番」
- 🎬 **多格式支援** - 支援 M3U8 (HLS)、MP4、TS 等影片格式
- 🌐 **網頁提取** - 使用 PuppeteerSharp 無頭瀏覽器自動從網頁提取影片連結
- 📚 **Hanime1 批量下載** - 依年月（預設上個月）或關鍵字搜尋裏番清單，支援單選 / 範圍 / 多選 / 全選後批次下載
- 🏷️ **自動命名** - jable.tv 自動從 URL 取得檔名；Hanime1 自動以影片標題命名
- ⚡ **並行下載** - 多執行緒並行下載 TS 片段或檔案分段（預設 10 個並行連線）
- 🔐 **AES-128 解密** - 自動處理加密的 M3U8 串流 (AES-128-CBC)
- 📊 **即時進度顯示** - 顯示下載進度、速度、剩餘時間
- 🌐 **Unicode 支援** - 完整支援中文、日文等多國語言檔名
- 🔄 **分段續傳** - 支援 HTTP Range 請求，適合大檔案下載
- 🎞️ **自動轉檔** - 使用 FFMpegCore 自動合併片段並轉換為 MP4
- 🛠️ **FFmpeg 自動安裝** - 未偵測到 FFmpeg 時，可自動透過 winget / Chocolatey 安裝，或直接下載預編譯版並加入 PATH
- 📁 **可設定輸出路徑** - 透過 `appsettings.json` 指定下載資料夾
- ⏭️ **跳過已存在檔案** - 目標 MP4 已存在時自動略過，方便續傳整批清單

### VideoPlayer（播放器，Windows 限定）

- 🗂️ **本機資料夾瀏覽** - 選擇資料夾後掃描其中的影片
- 🖼️ **縮圖預覽** - 顯示影片縮圖、檔名、大小與修改日期
- 🔀 **格狀 / 清單檢視切換**
- 🧭 **資料夾導航** - 在子資料夾之間瀏覽
- ⏯️ **記憶播放位置** - 透過 `log.json` 記錄每部影片的播放進度，下次接續播放

## 系統需求

### HentaiDownloader（下載器）

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) 或更高版本
- [FFmpeg](https://ffmpeg.org/download.html)（M3U8 下載與影片轉檔需要）
  - 程式啟動時會自動偵測；若未安裝可選擇自動安裝（winget / Chocolatey / 下載預編譯版），毋須手動設定 PATH

### VideoPlayer（播放器）

- Windows 10 (build 17763 / 1809) 以上或 Windows 11（**僅限 Windows**）
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)（目標框架 `net8.0-windows`）
- Windows App SDK（WinUI 3，已透過 NuGet 引用）
- 建議使用 Visual Studio 2022（含「.NET 桌面開發」工作負載）

## 快速開始

### 安裝

```bash
# 複製專案
git clone https://github.com/s780609/HentaiDownloader.git
cd HentaiDownloader

# 建置整個解決方案（Windows）
dotnet build

# 僅執行下載器（跨平台）
dotnet run --project HentaiDownloader
```

> VideoPlayer 為 Windows 專屬，無法在 Linux / macOS 建置或執行。

### 使用方式（下載器）

執行後會先選擇下載模式：

```
========== 請選擇下載模式 ==========
[1] 手動輸入 URL 下載
[2] 批量下載 Hanime1 上個月裏番
=====================================
請選擇 (1 或 2):
```

#### 模式 1：手動輸入 URL 下載

1. 輸入影片 URL（可為 M3U8 / MP4 / TS 等直接連結，或一般網頁連結）
2. 程式自動判斷：直接連結直接下載，網頁連結則用 PuppeteerSharp 提取影片
3. 輸入輸出檔名（支援中文 / 日文 / 空格；jable.tv 會自動帶入檔名）
4. 下載完成後可選擇是否繼續下一部

```
請輸入 URL: https://example.com/video/playlist.m3u8
✅ 偵測到直接影片連結
請輸入輸出檔案名稱 (不含副檔名，支援中文/日文/空格): 我的影片
偵測到 M3U8 連結...
找到 150 個片段
使用 10 個並行下載
下載進度: 150/150 (100.0%) | 速度: 8.5 片段/秒 | 剩餘: 0 秒
下載完成! 耗時: 17.6 秒
正在合併片段並轉換為 MP4...
✅ 下載完成: C:\Downloads\我的影片.mp4
```

#### 模式 2：批量下載 Hanime1 裏番

1. （可選）輸入搜尋關鍵字（支援日文）
2. 選擇年份與月份（預設上個月；有輸入關鍵字時可輸入 `skip` 不限制日期）
3. 從清單選擇要下載的影片：
   - 單一：`1`
   - 範圍：`1-5`
   - 多選：`1,3,5`
   - 全部：`all`
   - 取消：`q`
4. 批次下載（整批共用同一個瀏覽器實例，避免反覆啟動造成卡住）

### 使用方式（播放器）

1. 以 Visual Studio 2022 開啟 `HentaiDownloader.sln`，將 `VideoPlayer` 設為啟始專案後執行；或：
   ```bash
   dotnet run --project VideoPlayer
   ```
2. 選擇影片資料夾
3. 點選縮圖播放，並可切換格狀 / 清單檢視與瀏覽子資料夾

## 設定（appsettings.json）

下載器的輸出路徑可在 `HentaiDownloader/appsettings.json` 設定：

```json
{
  "DownloadSettings": {
    "OutputPath": "C:\\Users\\你的帳號\\Documents\\H"
  }
}
```

- 若未設定或讀取失敗，預設輸出到執行目錄下的 `影片/` 資料夾。
- 設定的資料夾若不存在會自動建立。

## 支援的網站

| 網站 | 提取方式 | 畫質選擇 | 自動命名 |
|------|----------|----------|----------|
| hanime1.me | 批量清單 + 網頁提取 | 自動挑最高（1080p → 720p → 480p → 360p） | 以影片標題命名 |
| jable.tv | 網頁提取 | 取 `/{5碼數字}.m3u8` 連結；若為 master playlist 自動挑最高畫質 | 從 URL 自動帶入 |
| 其他網站 | 直接連結或通用網頁提取 | M3U8 為 master playlist 時自動挑最高畫質 | 需手動輸入 |

> 註：下載 M3U8 時若偵測到 master playlist（含多畫質變體），會依 `BANDWIDTH`（無則用 `RESOLUTION`）自動挑選最高畫質的子清單，對所有網站皆適用。

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
├── HentaiDownloader.sln              # Visual Studio 解決方案檔案
├── README.md                         # 說明文件
├── HentaiDownloader/                 # 下載器（.NET 10 主控台）
│   ├── HentaiDownloader.csproj
│   ├── Program.cs                    # 主程式 / 模式選擇
│   ├── appsettings.json              # 下載路徑等設定
│   ├── Services/
│   │   ├── AppSettings.cs            # 讀取 appsettings.json 設定
│   │   ├── BrowserHelper.cs          # 瀏覽器輔助
│   │   ├── ConsoleService.cs         # 控制台 / Unicode 輸入輸出
│   │   ├── DownloadService.cs        # 下載、解密、合併
│   │   ├── FFmpegService.cs          # FFmpeg 檢查 / 自動安裝 / 合併
│   │   ├── Hanime1Service.cs         # Hanime1 清單抓取與選取
│   │   ├── UserInputService.cs       # 使用者輸入 / 自動命名
│   │   └── VideoExtractorService.cs  # 網頁影片 URL 提取與畫質選擇
│   └── Models/
│       └── M3U8Info.cs               # M3U8 資訊模型
└── VideoPlayer/                      # 播放器（WinUI 3，Windows 限定）
    ├── VideoPlayer.csproj
    ├── App.xaml(.cs)                 # 應用程式進入點
    ├── MainWindow.xaml(.cs)          # 主視窗
    ├── Converters/                   # XAML 轉換器
    ├── Models/                       # FolderItem / VideoGroup / VideoItem
    ├── Services/
    │   └── PlaybackLogService.cs     # 播放位置紀錄（log.json）
    └── ViewModels/
        └── MainViewModel.cs          # 主畫面 ViewModel
```

## 技術細節

### 下載策略

- **網頁提取**: 使用 PuppeteerSharp 無頭瀏覽器載入網頁，攔截網路請求與 video 標籤取得影片 URL；批量模式下整批共用同一瀏覽器實例
- **畫質選擇**: hanime1.me 依 1080p → 720p → 480p → 360p 挑最高；jable.tv 取符合 `/{5碼}.m3u8` 的連結
- **M3U8 master playlist**: 下載到的 m3u8 若為 master playlist，會依 `BANDWIDTH`（無則用 `RESOLUTION`）自動挑最高畫質的子清單後再下載
- **M3U8 串流**: 解析 M3U8 播放清單，並行下載所有 TS 片段，最後使用 FFMpegCore（失敗時回退直接呼叫 FFmpeg）合併
- **直接連結**: 檢查伺服器是否支援 Range 請求，支援則分段並行下載，否則使用串流下載
- **自動轉檔**: 下載非 MP4 格式時，自動轉換為 MP4

### 加密處理

支援 AES-128-CBC 加密的 M3U8 串流：
- 自動從 `#EXT-X-KEY` 標籤取得金鑰 URL 與 IV
- 支援自訂 IV 或使用預設 IV（以片段序號產生）
- 下載後自動解密每個片段

### 使用的技術與套件

**HentaiDownloader**
- **PuppeteerSharp 20.2.5**: 無頭瀏覽器，用於網頁解析與影片 URL 提取
- **FFMpegCore 5.1.0**: FFmpeg 的 .NET 封裝，用於影片合併與轉檔
- **Microsoft.Extensions.Configuration / .Json 9.0.0**: 讀取 `appsettings.json` 設定

**VideoPlayer**
- **Microsoft.WindowsAppSDK 1.8.x**: WinUI 3 桌面 UI 框架
- **Microsoft.Windows.SDK.BuildTools**: Windows SDK 建置工具

## 開發環境設定

1. 安裝 [.NET 10.0 SDK](https://dotnet.microsoft.com/download)（下載器）與 [.NET 8.0 SDK](https://dotnet.microsoft.com/download)（播放器）
2. （選用）安裝 [FFmpeg](https://ffmpeg.org/download.html)，或讓程式於首次執行時自動安裝
3. 使用 Visual Studio 2022 或 VS Code 開啟 `HentaiDownloader.sln`
4. 還原 NuGet 套件：`dotnet restore`
5. 建置專案：`dotnet build`

## 打包成 EXE

將專案打包成 self-contained 單一 EXE 檔案，可在沒有安裝 .NET 的電腦上直接執行。

### HentaiDownloader

```bash
cd HentaiDownloader
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "../publish"
```

### VideoPlayer

```bash
cd VideoPlayer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "../publish"
```

### 輸出結果

打包完成後，EXE 檔案會在 `publish` 資料夾中：

| 檔案 | 說明 |
|------|------|
| `HentaiDownloader.exe` | 影片下載器 |
| `VideoPlayer.exe` | 影片播放器 |

> **注意**: 打包後可刪除 `.pdb` 檔案以減少檔案數量。

## 授權

此專案尚未指定授權。

## 貢獻

歡迎提交 Issue 和 Pull Request！
