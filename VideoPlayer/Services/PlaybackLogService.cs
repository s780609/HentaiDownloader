using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace VideoPlayer2.Services;

/// <summary>
/// 播放紀錄服務，用於儲存和讀取影片播放位置
/// </summary>
public class PlaybackLogService
{
    private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "log.json");
    private Dictionary<string, PlaybackRecord> _records = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 載入播放紀錄
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(LogFilePath))
            {
                var json = await File.ReadAllTextAsync(LogFilePath);
                var records = JsonSerializer.Deserialize<Dictionary<string, PlaybackRecord>>(json);
                if (records != null)
                {
                    _records = records;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入播放紀錄失敗: {ex.Message}");
            _records = new Dictionary<string, PlaybackRecord>();
        }
    }

    /// <summary>
    /// 儲存播放紀錄
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_records, JsonOptions);
            await File.WriteAllTextAsync(LogFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"儲存播放紀錄失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 取得影片的播放位置（秒）
    /// </summary>
    /// <param name="filePath">影片檔案路徑</param>
    /// <returns>上次播放位置（秒），如果沒有紀錄則回傳 0</returns>
    public double GetPlaybackPosition(string filePath)
    {
        if (_records.TryGetValue(filePath, out var record))
        {
            return record.PositionSeconds;
        }
        return 0;
    }

    /// <summary>
    /// 設定影片的播放位置
    /// </summary>
    /// <param name="filePath">影片檔案路徑</param>
    /// <param name="positionSeconds">播放位置（秒）</param>
    public void SetPlaybackPosition(string filePath, double positionSeconds)
    {
        _records[filePath] = new PlaybackRecord
        {
            FilePath = filePath,
            PositionSeconds = positionSeconds,
            LastUpdated = DateTime.Now
        };
    }

    /// <summary>
    /// 移除影片的播放紀錄（當影片播放完畢時）
    /// </summary>
    /// <param name="filePath">影片檔案路徑</param>
    public void RemovePlaybackPosition(string filePath)
    {
        _records.Remove(filePath);
    }

    /// <summary>
    /// 儲存並更新播放位置
    /// </summary>
    public async Task SavePlaybackPositionAsync(string filePath, double positionSeconds)
    {
        SetPlaybackPosition(filePath, positionSeconds);
        await SaveAsync();
    }
}

/// <summary>
/// 播放紀錄項目
/// </summary>
public class PlaybackRecord
{
    /// <summary>
    /// 影片檔案路徑
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 播放位置（秒）
    /// </summary>
    public double PositionSeconds { get; set; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
