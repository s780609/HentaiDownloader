using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace VideoPlayer2.Services;

/// <summary>
/// 配置服務，用於讀取 appsettings.json
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    /// <summary>
    /// 取得照片路徑
    /// </summary>
    public string GetPhotoPath()
    {
        return _configuration["PhotoSettings:PhotoPath"] ?? "C:\\Photos";
    }

    /// <summary>
    /// 取得支援的圖片副檔名
    /// </summary>
    public List<string> GetSupportedImageExtensions()
    {
        var extensions = _configuration.GetSection("PhotoSettings:SupportedExtensions").Get<List<string>>();
        return extensions ?? new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    }
}
