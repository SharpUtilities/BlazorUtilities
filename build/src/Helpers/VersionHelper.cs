using System;
using System.Text.Json;
using Nuke.Common.IO;

namespace Build.Helpers;

public static class VersionHelper
{
    public static string CalculateVersion(
        AbsolutePath projectDirectory,
        AbsolutePath rootDirectory,
        Func<AbsolutePath, int> getCommitCount,
        string? prereleaseSuffix = null)
    {
        var versionFile = projectDirectory / "package-version.json";

        if (!versionFile.FileExists())
        {
            versionFile = rootDirectory / "package-version.json";
        }

        if (!versionFile.FileExists())
        {
            return "1.0.0";
        }

        var json = versionFile.ReadAllText();
        return CalculateVersionFromJson(json, getCommitCount(projectDirectory), prereleaseSuffix);
    }

    public static string CalculateVersionFromJson(string json, int commitCount, string? prereleaseSuffix = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var major = root.GetProperty("major").GetInt32();
        var minor = root.GetProperty("minor").GetInt32();
        var patch = root.TryGetProperty("patch", out var patchProp) ? patchProp.GetInt32() : 0;
        var prerelease = root.TryGetProperty("prerelease", out var preProp) ? preProp.GetString() : null;

        // If requested, append an extra prerelease segment (e.g., alpha.ea)
        if (!string.IsNullOrWhiteSpace(prereleaseSuffix))
        {
            prerelease = string.IsNullOrWhiteSpace(prerelease)
                ? prereleaseSuffix
                : $"{prerelease}.{prereleaseSuffix}";
        }

        if (string.IsNullOrEmpty(prerelease))
        {
            return $"{major}.{minor}.{commitCount}";
        }

        return $"{major}.{minor}.{patch}-{prerelease}.{commitCount}";
    }

    public static string? ExtractVersionFromTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        var versionStart = tag.LastIndexOf("/v", StringComparison.Ordinal);
        return versionStart >= 0 ? tag[(versionStart + 2)..] : null;
    }
}
