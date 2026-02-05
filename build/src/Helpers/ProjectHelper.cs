using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common.ProjectModel;

namespace Build.Helpers;

internal static class ProjectHelper
{
    public static bool HasValidDirectory(Project project)
    {
        return project.Directory != null && project.Path != null;
    }

    public static bool IsInSourceDirectory(Project project, char directorySeparator)
    {
        return project.Path?.ToString().Contains($"{directorySeparator}src{directorySeparator}") == true;
    }

    public static bool IsPackable(Project project)
    {
        var isPackable = project.GetProperty<bool?>("IsPackable");
        var isTestProject = project.GetProperty<bool?>("IsTestProject");

        if (isPackable == false || isTestProject == true)
        {
            return false;
        }

        var packageRefs = project.GetItems<string>("PackageReference") ?? [];

        return !IsTestProject(packageRefs);
    }

    public static bool IsTestProject(IEnumerable<string> packageReferences)
    {
        // Shitty way of testing this...
        return packageReferences.Any(p =>
            p.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("nunit", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("mstest", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase));
    }

    public static string[] GetDependencies(Project project)
    {
        var projectRefs = project.GetItems<string>("ProjectReference") ?? [];
        return projectRefs
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .ToArray()!;
    }

    public static string GetPackageId(Project project)
    {
        return project.GetProperty<string>("PackageId") ?? project.Name;
    }
}
