using System.Collections.Generic;
using System.Linq;
using Nuke.Common.IO;
using static Nuke.Common.Tools.Git.GitTasks;

namespace Build.Helpers;

internal static class GitHelper
{
    public static string? GetLastTagForProject(string packageId)
    {
        try
        {
            var result = Git($"tag --list \"{packageId}/v*\" --sort=-version:refname", logOutput: false);

            if (result.Count == 0)
            {
                return null;
            }

            var text = result.First().Text;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    public static string? GetLastVersionForProject(string packageId)
    {
        var tag = GetLastTagForProject(packageId);
        return VersionHelper.ExtractVersionFromTag(tag);
    }

    public static List<string> GetChangedFilesSinceTag(string tagName, string projectDir)
    {
        try
        {
            var result = Git($"diff --name-only {tagName} HEAD -- \"{projectDir}\"", logOutput: false);
            return result
                .Select(x => x.Text)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static int GetCommitCount(AbsolutePath path)
    {
        try
        {
            var result = Git($"rev-list --count HEAD -- \"{path}\"", logOutput: false);
            return int.Parse(result.First().Text.Trim());
        }
        catch
        {
            return 0;
        }
    }

    public static bool TagExists(string tagName)
    {
        try
        {
            var result = Git("tag --list", logOutput: false);
            return result.Any(x => x.Text == tagName);
        }
        catch
        {
            return false;
        }
    }

    public static void CreateTag(string tagName, string message)
    {
        Git($"tag -a \"{tagName}\" -m \"{message}\"");
    }
}
