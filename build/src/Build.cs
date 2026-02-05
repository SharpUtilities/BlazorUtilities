using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Build.Helpers;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Build;

[GitHubActions(
    "ci",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    FetchDepth = 0,
    OnPushBranches = ["main"],
    OnPushExcludePaths = ["*.md", ".gitignore", "LICENSE"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(Validate)],
    EnableGitHubToken = true,
    WritePermissions = [GitHubActionsPermissions.Contents, GitHubActionsPermissions.Packages]
)]
internal sealed class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build")]
    private readonly Configuration Configuration = Configuration.Release;

    [Solution]
    private readonly Solution Solution = null!;

    [GitRepository]
    private readonly GitRepository GitRepository = null!;

    [Parameter("NuGet source URL")]
    private readonly string NugetSource = "https://nuget.pkg.github.com/SharpUtilities/index.json";

    [Parameter("NuGet API key")]
    [Secret]
    private readonly string NugetApiKey = null!;

    [Parameter("GitHub token")]
    [Secret]
    private readonly string GitHubToken = null!;

    [Parameter("If true, builds/packages but does NOT push to any feed (dry run)")]
    private readonly bool SkipPush;

    [Parameter("If true, will NOT create git tags (useful for dry runs)")]
    private readonly bool SkipTags;

    [Parameter("Extra prerelease suffix for GitHub Packages builds (e.g. 'ea' => alpha.ea). Empty disables suffixing.")]
    private readonly string GitHubPrereleaseSuffix = "ea";

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PackagesDirectory => OutputDirectory / "packages";

    // State
    readonly Dictionary<string, string> PublishedVersions = new();
    readonly HashSet<string> ProjectsNeedingBuild = [];
    readonly List<string> PublishedList = [];
    List<Project> PackableProjects = [];

    // ============================================
    // Core Targets
    // ============================================

    Target Clean => td => td
        .Description("Clean build outputs")
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => td => td
        .Description("Restore NuGet packages")
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(Solution));
        });

    Target Compile => td => td
        .Description("Compile the solution")
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => td => td
        .Description("Run all tests")
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    // ============================================
    // CI Entry Points
    // ============================================

    Target Validate => td => td
        .Description("PR validation - compile and run tests")
        .DependsOn(Test)
        .Executes(() =>
        {
            Log.Information("✅ Validation complete - all tests passed");
        });

    void AssertPublishingAllowed()
    {
        // Prefer GITHUB_REF when running in GitHub Actions
        var githubRef = Environment.GetEnvironmentVariable("GITHUB_REF");

        var isMain =
            string.Equals(githubRef, "refs/heads/main", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(GitRepository.Branch, "main", StringComparison.OrdinalIgnoreCase);

        if (!isMain)
        {
            Assert.Fail("Publishing is only allowed from 'main'.");
        }
    }

    Target PublishToGitHub => td => td
        .Description("Publish changed packages to GitHub Packages")
        .DependsOn(Test)
        .Executes(async () =>
        {
            AssertPublishingAllowed();

            if (SkipPush)
            {
                Log.Information("🧪 DRY RUN: SkipPush=true — will pack, but NOT push packages.");
            }
            else
            {
                Assert.True(!string.IsNullOrWhiteSpace(GitHubToken),
                    "GitHub token is required unless SkipPush=true. Pass --github-token <TOKEN> or set SkipPush.");
            }

            if (SkipTags)
            {
                Log.Information("🏷️  SkipTags=true — will NOT create git tags (but will show what they would be).");
            }

            PackagesDirectory.CreateOrCleanDirectory();

            DiscoverPackableProjects();
            DetectChanges();
            PropagateDependencies();
            PublishPackagesAsync(NugetSource, GitHubToken, skipPush: SkipPush, prereleaseSuffix: GitHubPrereleaseSuffix);

            if (SkipTags)
            {
                PreviewTags();
            }
            else
            {
                CreateTags();
            }

            if (PublishedList.Count > 0)
            {
                var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
                if (!string.IsNullOrEmpty(githubOutput))
                {
                    await File.AppendAllTextAsync(githubOutput,
                        $"published-packages={string.Join(",", PublishedList)}\n");
                }
            }
        });

    Target PublishToNuGet => td => td
        .Description("Publish packages to NuGet.org (not yet implemented)")
        .DependsOn(Test)
        .Executes(() =>
        {
            AssertPublishingAllowed();

            Log.Information("========================================");
            Log.Information("🚀 NUGET.ORG PUBLISH");
            Log.Information("========================================");
            Log.Information("");
            Log.Information("⚠️  NuGet.org publishing is not yet configured.");
            Log.Information("");
            Log.Information("========================================");
        });

    // ============================================
    // Project Discovery
    // ============================================

    void DiscoverPackableProjects()
    {
        Log.Information("========================================");
        Log.Information("🔎 DISCOVERING PROJECTS");
        Log.Information("========================================");

        PackableProjects = Solution.AllProjects
            .Where(ProjectHelper.HasValidDirectory)
            .Where(p => ProjectHelper.IsInSourceDirectory(p, Path.DirectorySeparatorChar))
            .Where(ProjectHelper.IsPackable)
            .ToList();

        PackableProjects = TopologicalSorter.Sort(PackableProjects, ProjectHelper.GetDependencies);

        foreach (var project in PackableProjects)
        {
            var deps = ProjectHelper.GetDependencies(project);
            Log.Information("✅ {Project}: Found ({Count} dependencies)", project.Name, deps.Length);
        }

        Log.Information("");

        if (PackableProjects.Count > 0)
        {
            Log.Information("📦 Build order: {Order}", string.Join(" → ", PackableProjects.Select(p => p.Name)));
        }
        else
        {
            Log.Information("📦 No packable projects found");
        }

        Log.Information("");
    }

    // ============================================
    // Change Detection
    // ============================================

    void DetectChanges()
    {
        Log.Information("========================================");
        Log.Information("🔍 DETECTING CHANGES");
        Log.Information("========================================");

        foreach (var project in PackableProjects)
        {
            var packageId = ProjectHelper.GetPackageId(project);
            var lastTag = GitHelper.GetLastTagForProject(packageId);

            if (lastTag == null)
            {
                Log.Information("✅ {Project}: No previous tag - marking as changed", project.Name);
                ProjectsNeedingBuild.Add(project.Name);
                continue;
            }

            // Directory is guaranteed non-null due to HasValidDirectory filter
            var projectDir = project.Directory!.ToString();
            var changedFiles = GitHelper.GetChangedFilesSinceTag(lastTag, projectDir);

            if (changedFiles.Count > 0)
            {
                Log.Information("✅ {Project}: {Count} file(s) changed since {Tag}",
                    project.Name, changedFiles.Count, lastTag);
                ProjectsNeedingBuild.Add(project.Name);
            }
            else
            {
                Log.Information("⏭️  {Project}: No changes since {Tag}", project.Name, lastTag);
            }
        }

        Log.Information("");
    }

    void PropagateDependencies()
    {
        Log.Information("========================================");
        Log.Information("🔄 CHECKING DEPENDENCIES");
        Log.Information("========================================");

        var changesMade = true;

        while (changesMade)
        {
            changesMade = false;

            foreach (var project in PackableProjects)
            {
                if (ProjectsNeedingBuild.Contains(project.Name))
                {
                    continue;
                }

                foreach (var dep in ProjectHelper.GetDependencies(project))
                {
                    if (ProjectsNeedingBuild.Contains(dep))
                    {
                        Log.Information("🔄 {Project} needs rebuild (dependency {Dep} changed)",
                            project.Name, dep);
                        ProjectsNeedingBuild.Add(project.Name);
                        changesMade = true;
                        break;
                    }
                }
            }
        }

        Log.Information("");
    }

    // ============================================
    // Publishing
    // ============================================

    void PublishPackagesAsync(string source, string apiKey, bool skipPush, string? prereleaseSuffix)
    {
        Log.Information("========================================");
        Log.Information("📦 PUBLISHING PACKAGES");
        Log.Information("========================================");

        var anyPublished = false;

        foreach (var project in PackableProjects.Where(project => ProjectsNeedingBuild.Contains(project.Name)))
        {
            anyPublished = true;
            var packageId = ProjectHelper.GetPackageId(project);

            // Directory is guaranteed non-null due to HasValidDirectory filter
            var version = VersionHelper.CalculateVersion(
                project.Directory!,
                RootDirectory,
                GitHelper.GetCommitCount,
                prereleaseSuffix);

            Log.Information("");
            Log.Information("📦 Packing {Project} v{Version}", project.Name, version);

            var properties = new Dictionary<string, object>();

            foreach (var dep in ProjectHelper.GetDependencies(project))
            {
                var depProject = PackableProjects.FirstOrDefault(p => p.Name == dep);
                if (depProject == null)
                {
                    continue;
                }

                if (PublishedVersions.TryGetValue(dep, out var depVersion))
                {
                    Log.Information("  └── {Dep} v{Version}", dep, depVersion);
                    properties[$"{dep}Version"] = depVersion;
                }
                else
                {
                    var depPackageId = ProjectHelper.GetPackageId(depProject);
                    var lastDepVersion = GitHelper.GetLastVersionForProject(depPackageId);

                    if (lastDepVersion != null)
                    {
                        Log.Information("  └── {Dep} v{Version} (existing)", dep, lastDepVersion);
                        properties[$"{dep}Version"] = lastDepVersion;
                    }
                    else
                    {
                        Log.Warning("  ⚠️  No version found for dependency {Dep}", dep);
                    }
                }
            }

            DotNetPack(s => s
                .SetProject(project)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(PackagesDirectory)
                .SetVersion(version)
                .EnableNoBuild()
                .SetProperties(properties)
                .SetProperty("PackageVersion", version)
                .SetProperty("Version", version));

            var packageFile = PackagesDirectory.GlobFiles($"{packageId}.{version}.nupkg").FirstOrDefault()
                              ?? PackagesDirectory.GlobFiles($"*.{version}.nupkg").FirstOrDefault();

            if (packageFile == null)
            {
                Log.Warning("⚠️  Package file not found for {PackageId}", packageId);
                continue;
            }

            if (skipPush)
            {
                Log.Information("🧪 DRY RUN: Produced package {Package} (not pushing).", packageFile.Name);

                // In dry-run, still record the version so tag preview works.
                PublishedVersions[project.Name] = version;
                PublishedList.Add($"{packageId}:{version}");

                continue;
            }

            Log.Information("🚀 Publishing {Package}", packageFile.Name);

            DotNetNuGetPush(s => s
                .SetTargetPath(packageFile)
                .SetSource(source)
                .SetApiKey(apiKey)
                .EnableSkipDuplicate());

            PublishedVersions[project.Name] = version;
            PublishedList.Add($"{packageId}:{version}");

            packageFile.DeleteFile();
        }

        if (!anyPublished)
        {
            Log.Information("No packages need publishing - no changes detected");
        }

        Log.Information("");
        Log.Information("========================================");
    }

    void PreviewTags()
    {
        if (PublishedVersions.Count == 0)
        {
            Log.Information("🏷️  No tags would be created (no packages were produced).");
            return;
        }

        Log.Information("========================================");
        Log.Information("🏷️  TAG PREVIEW (dry run)");
        Log.Information("========================================");

        foreach (var (projectName, version) in PublishedVersions)
        {
            var project = PackableProjects.First(p => p.Name == projectName);
            var packageId = ProjectHelper.GetPackageId(project);
            var tag = $"{packageId}/v{version}";
            Log.Information("🏷️  Would create tag: {Tag}", tag);
        }

        Log.Information("");
    }

    void CreateTags()
    {
        if (PublishedVersions.Count == 0)
        {
            return;
        }

        Log.Information("========================================");
        Log.Information("🏷️  CREATING TAGS");
        Log.Information("========================================");

        foreach (var (projectName, version) in PublishedVersions)
        {
            var project = PackableProjects.First(p => p.Name == projectName);
            var packageId = ProjectHelper.GetPackageId(project);
            var tag = $"{packageId}/v{version}";

            if (!GitHelper.TagExists(tag))
            {
                GitHelper.CreateTag(tag, $"Release {packageId} v{version}");
                Log.Information("🏷️  Created tag: {Tag}", tag);
            }
            else
            {
                Log.Information("🏷️  Tag {Tag} already exists", tag);
            }
        }

        Log.Information("");
    }
}
