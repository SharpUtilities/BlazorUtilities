using Build.Helpers;

namespace Build.Tests;

public sealed class GitHelperTests
{
    // ============================================
    // Note: These tests verify the helper methods that don't require git.
    // Methods that call Git directly (GetLastTagForProject, etc.)
    // would need integration tests with a real git repo.
    // ============================================

    // ============================================
    // GetLastVersionForProject Tests (via VersionHelper)
    // ============================================

    [Theory]
    [InlineData("ProjectA/v1.2.3", "1.2.3")]
    [InlineData("Package/v0.0.1", "0.0.1")]
    [InlineData("My.Package/v10.20.30-beta.5", "10.20.30-beta.5")]
    public void ExtractVersionFromTag_ShouldWork_WhenUsedByGitHelper(string tag, string expected)
    {
        // This tests the VersionHelper method that GitHelper depends on
        // Arrange & Act
        var result = VersionHelper.ExtractVersionFromTag(tag);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void ExtractVersionFromTag_ShouldReturnNull_WhenInvalidTag(string? tag)
    {
        // Arrange & Act
        var result = VersionHelper.ExtractVersionFromTag(tag);

        // Assert
        Assert.Null(result);
    }
}
