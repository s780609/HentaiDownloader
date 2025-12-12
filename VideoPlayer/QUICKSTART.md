# VideoPlayer Quick Start Guide

快速開始指南 - 5 分鐘內啟動並運行 VideoPlayer

## 最快速的方式開始

### 1. 確認系統需求
- ✅ Windows 10 (1809+) 或 Windows 11
- ✅ Visual Studio 2022
- ✅ .NET 10.0 SDK

### 2. 開啟專案
```bash
# 在 Windows 上
cd HentaiDownloader
start HentaiDownloader.sln
```

### 3. 建置並執行
1. 在 Visual Studio 中，選擇 **VideoPlayer** 作為啟動專案
2. 選擇平台：**x64** 或 **ARM64**
3. 按 **F5** 執行

### 4. 開始使用
1. 點擊 **「Select Folder」** 按鈕
2. 選擇包含影片的資料夾
3. 等待掃描完成
4. 點擊任何影片縮圖開始播放

## 專案結構一覽

```
VideoPlayer/
├── App.xaml/cs              # 應用程式進入點
├── MainWindow.xaml/cs       # 主視窗 (導航)
├── Views/                   # UI 頁面
│   ├── VideoGalleryPage.*   # 影片畫廊
│   └── VideoPlayerPage.*    # 影片播放器
├── ViewModels/              # MVVM ViewModels
│   ├── VideoGalleryViewModel.cs
│   └── VideoPlayerViewModel.cs
├── Models/                  # 資料模型
│   └── VideoItem.cs
├── Services/                # 業務邏輯
│   ├── VideoScannerService.cs
│   └── ThumbnailService.cs
└── Assets/                  # 資源檔案
```

## 核心功能

### 影片瀏覽
- 📁 選擇資料夾掃描影片
- 🖼️ 自動生成縮圖（使用 FFmpeg）
- 📊 顯示影片資訊（名稱、時長、大小）
- 🔄 支援子資料夾遞迴掃描

### 影片播放
- ▶️ 點擊縮圖即可播放
- ⏯️ 完整播放控制（播放/暫停/快進/快退）
- 🔊 音量控制與靜音
- 🖥️ 全螢幕模式
- ⚡ 播放速度調整

### 支援格式
MP4, MKV, AVI, WMV, MOV, FLV, WEBM, M4V, MPG, MPEG, 3GP

## 常見問題快速解答

### ❓ 建置失敗？
```bash
# 清除並重新建置
dotnet clean
dotnet restore
dotnet build
```

### ❓ 影片不播放？
- 確認影片編解碼器受 Windows Media Foundation 支援
- 嘗試安裝 K-Lite Codec Pack
- 檢查檔案權限

### ❓ 縮圖不顯示？
- 首次執行需要下載 FFmpeg（需要網路連線）
- 檢查 `%TEMP%` 資料夾寫入權限
- 使用「Clear Cache」清除快取後重試

### ❓ 應用程式無法啟動？
- 安裝 Windows App SDK Runtime:
  https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe
- 更新 Windows 到最新版本
- 安裝 Visual C++ Redistributables

## 技術亮點

### MVVM 架構
- 清晰的關注點分離
- 易於測試和維護
- 使用 CommunityToolkit.Mvvm

### 效能最佳化
- 批次載入影片（每次 10 個）
- 平行處理中繼資料和縮圖
- 持久化縮圖快取

### 現代 UI
- Fluent Design System
- 符合 Windows 11 設計語言
- 回應式佈局

## 詳細文件

需要更多資訊？查看這些文件：

- **README.md** - 專案概述與功能說明
- **SETUP.md** - 完整安裝與疑難排解指南
- **實作說明.md** - 架構設計與實作細節（中文）

## 開發建議

### 新增影片格式
編輯 `Services/VideoScannerService.cs`:
```csharp
private readonly string[] _supportedExtensions = 
{
    ".mp4", ".mkv", /* 在這裡新增格式 */
};
```

### 修改縮圖尺寸
編輯 `Services/ThumbnailService.cs`:
```csharp
size: new System.Drawing.Size(320, 180)  // 調整寬度和高度
```

### 新增頁面
1. 在 `Views/` 建立 XAML 頁面
2. 在 `ViewModels/` 建立對應 ViewModel
3. 在 `MainWindow.xaml.cs` 新增導航邏輯

## 疑難排解指令

```bash
# 檢查 .NET 版本
dotnet --version

# 檢查已安裝的 SDK
dotnet --list-sdks

# 清除 NuGet 快取
dotnet nuget locals all --clear

# 重新建置專案
dotnet clean && dotnet restore && dotnet build

# 檢查專案參考
dotnet list VideoPlayer/VideoPlayer.csproj reference
```

## 效能提示

### 大量影片載入慢？
- 這是正常的，首次載入會生成所有縮圖
- 第二次載入會使用快取，速度會快很多
- 考慮不要一次選擇太多影片（建議 < 500 個）

### 記憶體使用高？
- 定期使用「Clear Cache」清除縮圖快取
- 避免同時開啟多個播放器視窗

## 鍵盤快捷鍵

播放器頁面：
- **Space** - 播放/暫停
- **←/→** - 快退/快進
- **↑/↓** - 增加/減少音量
- **F** - 全螢幕
- **M** - 靜音
- **Esc** - 退出全螢幕

## 下一步

1. ✅ 選擇一個影片資料夾試試看
2. ✅ 探索不同的播放控制
3. ✅ 查看進階功能文件
4. ✅ 根據需求自訂應用程式
5. ✅ 提供回饋或報告問題

## 獲得幫助

- 📖 查看 SETUP.md 了解詳細疑難排解
- 📖 閱讀實作說明.md 了解架構設計
- 🐛 在 GitHub 上回報問題
- 💬 查看現有 Issues 尋找解決方案

---

享受使用 VideoPlayer！🎬
