using System.Text.Json;

namespace NetherGate.API.FileSystem;

/// <summary>
/// 服务器文件访问接口
/// 提供安全的服务器文件读写功能
/// </summary>
public interface IServerFileAccess
{
    /// <summary>
    /// 获取服务器根目录
    /// </summary>
    string ServerDirectory { get; }

    /// <summary>
    /// 读取文本文件
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    /// <returns>文件内容</returns>
    Task<string> ReadTextFileAsync(string relativePath);

    /// <summary>
    /// 写入文本文件
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    /// <param name="content">文件内容</param>
    /// <param name="backup">是否先备份原文件</param>
    Task WriteTextFileAsync(string relativePath, string content, bool backup = true);

    /// <summary>
    /// 读取 JSON 文件
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    Task<T?> ReadJsonFileAsync<T>(string relativePath) where T : class;

    /// <summary>
    /// 写入 JSON 文件
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    /// <param name="data">数据对象</param>
    /// <param name="backup">是否先备份原文件</param>
    Task WriteJsonFileAsync<T>(string relativePath, T data, bool backup = true) where T : class;

    /// <summary>
    /// 读取服务器配置（server.properties）
    /// </summary>
    Task<Dictionary<string, string>> ReadServerPropertiesAsync();

    /// <summary>
    /// 写入服务器配置（server.properties）
    /// </summary>
    /// <param name="properties">配置项</param>
    /// <param name="backup">是否先备份原文件</param>
    Task WriteServerPropertiesAsync(Dictionary<string, string> properties, bool backup = true);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    bool FileExists(string relativePath);

    /// <summary>
    /// 检查目录是否存在
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    bool DirectoryExists(string relativePath);

    /// <summary>
    /// 创建目录
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    void CreateDirectory(string relativePath);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    /// <param name="backup">是否先备份</param>
    Task DeleteFileAsync(string relativePath, bool backup = true);

    /// <summary>
    /// 列出目录中的文件
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    /// <param name="pattern">文件匹配模式（如 "*.json"）</param>
    /// <param name="recursive">是否递归搜索</param>
    List<string> ListFiles(string relativePath, string pattern = "*", bool recursive = false);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="relativePath">相对于服务器目录的路径</param>
    FileInfo GetFileInfo(string relativePath);
}

