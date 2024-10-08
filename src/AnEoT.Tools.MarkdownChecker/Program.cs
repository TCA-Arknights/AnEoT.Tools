﻿using System.CommandLine;
using Microsoft.Extensions.Logging;
using AnEoT.Tools.MarkdownChecker.Models;
using AnEoT.Tools.MarkdownChecker.Checkers;

AppLogger appLogger = new();

RootCommand rootCommand = new("为《回归线》期刊设计的 Markdown 分析器");
Command singleFileCommand = new("file", "对单个 Markdown 文件进行分析。");
Command folderCommand = new("folder", "对文件夹内的 Markdown 文件进行分析。");

Option<string> pathOption = new("--path", "目标文件或文件夹的路径。")
{
    IsRequired = true,
};
pathOption.AddValidator(result =>
{
    string? commandName = result.Parent?.Symbol.Name;
    string? path = result.Tokens.FirstOrDefault()?.Value;

    if (commandName == singleFileCommand.Name)
    {
        if (File.Exists(path) != true)
        {
            result.ErrorMessage = "目标文件不存在。";
        }

        string ext = Path.GetExtension(path) ?? string.Empty;

        if (!ext.Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            result.ErrorMessage = "目标文件不是 Markdown 文件。";
        }
    }
    else if (commandName == folderCommand.Name)
    {
        if (Directory.Exists(path) != true)
        {
            result.ErrorMessage = "目标文件夹不存在。";
        }
    }
});

Option<bool> recursiveOption = new("--recursive", "以递归方式，分析子文件夹内的文件。");

Option<string?> rootPathOption = new(
    "--root-path",
    "设置解析链接时使用的根路径。若不设置该项，则会将 Markdown 文件所在的文件夹路径当为根路径。");
rootPathOption.AddValidator(result =>
{
    string? path = result.Tokens.FirstOrDefault()?.Value;

    if (path is null)
    {
        return;
    }

    if (Directory.Exists(path) != true)
    {
        result.ErrorMessage = "目标文件夹不存在。";
    }
});

singleFileCommand.AddOption(pathOption);
singleFileCommand.AddOption(rootPathOption);

folderCommand.AddOption(pathOption);
folderCommand.AddOption(rootPathOption);
folderCommand.AddOption(recursiveOption);

rootCommand.Add(singleFileCommand);
rootCommand.Add(folderCommand);

singleFileCommand.SetHandler(async (filePath, rootPath) =>
{
    appLogger.LogNormalInfomation("检查已开始...");

    if (rootPath is null)
    {
        appLogger.LogWarningInfomation("未配置根路径，检查过程可能会出现错误。");
    }

    CheckResult result = await FileChecker.CheckSingleFile(filePath, rootPath);
    if (result.IsNoErrorOrWarning)
    {
        appLogger.LogNormalInfomation("检查完成，未发现错误。");
    }
    else if (result.HasError)
    {
        appLogger.LogErrorInfomation($"检查完成，发现 {result.ErrorCount} 个错误，{result.WarningCount} 个警告。");
    }
    else
    {
        appLogger.LogWarningInfomation($"检查完成，存在 {result.WarningCount} 个警告。");
    }
}, pathOption, rootPathOption);
folderCommand.SetHandler(async (path, rootPath, isRecursive) =>
{
    appLogger.LogNormalInfomation("检查已开始...");

    if (rootPath is null)
    {
        appLogger.LogWarningInfomation("未配置根路径，检查过程可能会出现错误。");
    }

    CheckResult result = await FolderChecker.CheckDirectory(path, rootPath, isRecursive);
    if (result.IsNoErrorOrWarning)
    {
        appLogger.LogNormalInfomation("检查完成，未发现错误。");
    }
    else if(result.HasError)
    {
        appLogger.LogErrorInfomation($"检查完成，发现 {result.ErrorCount} 个错误，{result.WarningCount} 个警告。");
    }
    else
    {
        appLogger.LogWarningInfomation($"检查完成，存在 {result.WarningCount} 个警告。");
    }
}, pathOption, rootPathOption, recursiveOption);

int result = await rootCommand.InvokeAsync(args);
return result;

internal sealed partial class AppLogger
{
    private readonly ILogger _logger;

    public AppLogger()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("主程序");
        _logger = logger;
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{msgInfo}")]
    public partial void LogNormalInfomation(string msgInfo);
    
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "{msgWarning}")]
    public partial void LogWarningInfomation(string msgWarning);
    
    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "{msgError}")]
    public partial void LogErrorInfomation(string msgError);
}