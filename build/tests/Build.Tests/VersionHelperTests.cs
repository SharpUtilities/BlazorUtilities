using Build.Helpers;

namespace Build.Tests;

public sealed class VersionHelperTests
{
    // ============================================
    // ExtractVersionFromTag Tests
    // ============================================

    [Theory]
    [InlineData("ProjectA/v1.2.3", "1.2.3")]
    [InlineData("ProjectA/v1.0.0-beta.5", "1.0.0-beta.5")]
    [InlineData("MyPackage/v10.20.30", "10.20.30")]
    [InlineData("Some.Package/v1.0.0", "1.0.0")]
    [InlineData("Package/v0.0.1", "0.0.1")]
    [InlineData("A/v1.0.0-alpha.1", "1.0.0-alpha.1")]
    [InlineData("Package.Name.Here/v2.0.0-rc.1", "2.0.0-rc.1")]
    public void ExtractVersionFromTag_ShouldReturnVersion_WhenValidFormat(string tagName, string expected)
    {
        // Act
        var result = VersionHelper.ExtractVersionFromTag(tagName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid-tag")]
    [InlineData("v1.0.0")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("ProjectA-v1.0.0")]
    [InlineData("ProjectA\\v1.0.0")]
    [InlineData("no-version-here")]
    public void ExtractVersionFromTag_ShouldReturnNull_WhenInvalidFormat(string? tagName)
    {
        // Act
        var result = VersionHelper.ExtractVersionFromTag(tagName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractVersionFromTag_ShouldReturnLastVersion_WhenMultipleSlashV()
    {
        // Arrange
        var tagName = "org/repo/v1.0.0/v2.0.0";

        // Act
        var result = VersionHelper.ExtractVersionFromTag(tagName);

        // Assert
        Assert.Equal("2.0.0", result);
    }

    // ============================================
    // CalculateVersionFromJson Tests - Release Versions
    // ============================================

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnReleaseVersion_WhenNoPrerelease()
    {
        // Arrange
        var json = """
            {
                "major": 2,
                "minor": 1
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 42);

        // Assert
        Assert.Equal("2.1.42", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnReleaseVersion_WhenPrereleaseIsNull()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "patch": 0,
                "prerelease": null
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 10);

        // Assert
        Assert.Equal("1.0.10", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnReleaseVersion_WhenPrereleaseIsEmpty()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "patch": 0,
                "prerelease": ""
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 10);

        // Assert
        Assert.Equal("1.0.10", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldUseCommitCountAsPatch_WhenReleaseVersion()
    {
        // Arrange
        var json = """
            {
                "major": 5,
                "minor": 3
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 999);

        // Assert
        Assert.Equal("5.3.999", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldHandleZeroCommitCount_WhenReleaseVersion()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 0);

        // Assert
        Assert.Equal("1.0.0", result);
    }

    // ============================================
    // CalculateVersionFromJson Tests - Prerelease Versions
    // ============================================

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnPrereleaseVersion_WhenBetaSpecified()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "patch": 0,
                "prerelease": "beta"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 15);

        // Assert
        Assert.Equal("1.0.0-beta.15", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnPrereleaseVersion_WhenAlphaSpecified()
    {
        // Arrange
        var json = """
            {
                "major": 0,
                "minor": 1,
                "patch": 0,
                "prerelease": "alpha"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 5);

        // Assert
        Assert.Equal("0.1.0-alpha.5", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldReturnPrereleaseVersion_WhenRcSpecified()
    {
        // Arrange
        var json = """
            {
                "major": 2,
                "minor": 0,
                "patch": 0,
                "prerelease": "rc"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 3);

        // Assert
        Assert.Equal("2.0.0-rc.3", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldIncludePatchInPrerelease_WhenPatchSpecified()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 2,
                "patch": 3,
                "prerelease": "beta"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 50);

        // Assert
        Assert.Equal("1.2.3-beta.50", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldDefaultPatchToZero_WhenPatchMissingInPrerelease()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "prerelease": "alpha"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 7);

        // Assert
        Assert.Equal("1.0.0-alpha.7", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldHandleZeroCommitCount_WhenPrereleaseVersion()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "patch": 0,
                "prerelease": "alpha"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 0);

        // Assert
        Assert.Equal("1.0.0-alpha.0", result);
    }

    // ============================================
    // CalculateVersionFromJson Tests - Edge Cases
    // ============================================

    [Fact]
    public void CalculateVersionFromJson_ShouldHandleLargeVersionNumbers()
    {
        // Arrange
        var json = """
            {
                "major": 100,
                "minor": 200
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 99999);

        // Assert
        Assert.Equal("100.200.99999", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldHandleZeroMajorMinor()
    {
        // Arrange
        var json = """
            {
                "major": 0,
                "minor": 0
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 1);

        // Assert
        Assert.Equal("0.0.1", result);
    }

    [Fact]
    public void CalculateVersionFromJson_ShouldHandleComplexPrereleaseTag()
    {
        // Arrange
        var json = """
            {
                "major": 1,
                "minor": 0,
                "patch": 0,
                "prerelease": "preview"
            }
            """;

        // Act
        var result = VersionHelper.CalculateVersionFromJson(json, 42);

        // Assert
        Assert.Equal("1.0.0-preview.42", result);
    }
}
