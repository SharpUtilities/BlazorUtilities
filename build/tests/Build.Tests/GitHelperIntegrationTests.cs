using Build.Helpers;

namespace Build.Tests;

/// <summary>
/// Integration tests that require a real git repository.
/// These tests are skipped by default and can be run manually.
/// </summary>
public sealed class GitHelperIntegrationTests
{
    // ============================================
    // These tests require a real git repository to run.
    // They are marked with a trait to allow selective execution.
    // Run with: dotnet test --filter "Category=Integration"
    // ============================================

    [Fact]
    [Trait("Category", "Integration")]
    public void GetCommitCount_ShouldReturnNonNegative_WhenInGitRepository()
    {
        // This test only works when run from within a git repository
        // Skip if not in a git repo
        if (!Directory.Exists(".git") && !File.Exists(".git"))
        {
            return; // Skip - not in a git repo
        }

        // Arrange
        var path = Nuke.Common.NukeBuild.RootDirectory;

        // Act
        var result = GitHelper.GetCommitCount(path);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void TagExists_ShouldReturnFalse_WhenTagDoesNotExist()
    {
        // This test only works when run from within a git repository
        if (!Directory.Exists(".git") && !File.Exists(".git"))
        {
            return; // Skip - not in a git repo
        }

        // Arrange
        var nonExistentTag = $"NonExistent/v{Guid.NewGuid()}";

        // Act
        var result = GitHelper.TagExists(nonExistentTag);

        // Assert
        Assert.False(result);
    }
}
