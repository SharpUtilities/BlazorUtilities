using Build.Helpers;

namespace Build.Tests;

public sealed class TopologicalSorterTests
{
    // ============================================
    // Empty and Single Project Tests
    // ============================================

    [Fact]
    public void Sort_ShouldReturnEmptyList_WhenNoProjects()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>();

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Sort_ShouldReturnSingleProject_WhenOneProjectWithNoDependencies()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Single(result);
        Assert.Equal("ProjectA", result[0]);
    }

    [Fact]
    public void Sort_ShouldReturnSingleProject_WhenOneProjectWithEmptyDependencyArray()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = Array.Empty<string>()
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Single(result);
        Assert.Equal("ProjectA", result[0]);
    }

    // ============================================
    // Simple Dependency Tests
    // ============================================

    [Fact]
    public void Sort_ShouldOrderDependenciesFirst_WhenProjectHasDependency()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectB"],
            ["ProjectB"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ProjectB", result[0]);
        Assert.Equal("ProjectA", result[1]);
    }

    [Fact]
    public void Sort_ShouldHandleTransitiveDependencies_WhenChainExists()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectB"],
            ["ProjectB"] = ["ProjectC"],
            ["ProjectC"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("ProjectC", result[0]);
        Assert.Equal("ProjectB", result[1]);
        Assert.Equal("ProjectA", result[2]);
    }

    [Fact]
    public void Sort_ShouldHandleLongChain_WhenManyTransitiveDependencies()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["A"] = ["B"],
            ["B"] = ["C"],
            ["C"] = ["D"],
            ["D"] = ["E"],
            ["E"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("E", result[0]);
        Assert.Equal("D", result[1]);
        Assert.Equal("C", result[2]);
        Assert.Equal("B", result[3]);
        Assert.Equal("A", result[4]);
    }

    // ============================================
    // Multiple Dependencies Tests
    // ============================================

    [Fact]
    public void Sort_ShouldHandleMultipleDependencies_WhenProjectDependsOnMany()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectB", "ProjectC"],
            ["ProjectB"] = [],
            ["ProjectC"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("ProjectA", result[2]);
        Assert.Contains("ProjectB", result.Take(2));
        Assert.Contains("ProjectC", result.Take(2));
    }

    [Fact]
    public void Sort_ShouldHandleMultipleRoots_WhenNoSingleEntryPoint()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = [],
            ["ProjectB"] = [],
            ["ProjectC"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("ProjectA", result);
        Assert.Contains("ProjectB", result);
        Assert.Contains("ProjectC", result);
    }

    // ============================================
    // Diamond Dependency Tests
    // ============================================

    [Fact]
    public void Sort_ShouldHandleDiamondDependency_WhenDiamondExists()
    {
        // Arrange
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectB", "ProjectC"],
            ["ProjectB"] = ["ProjectD"],
            ["ProjectC"] = ["ProjectD"],
            ["ProjectD"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Equal("ProjectD", result[0]);
        Assert.Equal("ProjectA", result[3]);
        Assert.True(result.IndexOf("ProjectB") > result.IndexOf("ProjectD"));
        Assert.True(result.IndexOf("ProjectC") > result.IndexOf("ProjectD"));
        Assert.True(result.IndexOf("ProjectB") < result.IndexOf("ProjectA"));
        Assert.True(result.IndexOf("ProjectC") < result.IndexOf("ProjectA"));
    }

    [Fact]
    public void Sort_ShouldHandleComplexDiamond_WhenMultipleDiamonds()
    {
        // Arrange
        //       A
        //      /|\
        //     B C D
        //      \|/
        //       E
        var projects = new Dictionary<string, string[]>
        {
            ["A"] = ["B", "C", "D"],
            ["B"] = ["E"],
            ["C"] = ["E"],
            ["D"] = ["E"],
            ["E"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal("E", result[0]);
        Assert.Equal("A", result[4]);
    }

    // ============================================
    // Missing Dependencies Tests
    // ============================================

    [Fact]
    public void Sort_ShouldIgnoreMissingDependencies_WhenDependencyNotInList()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["MissingProject"]
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Single(result);
        Assert.Equal("ProjectA", result[0]);
    }

    [Fact]
    public void Sort_ShouldIgnoreMultipleMissingDependencies_WhenSomeDependenciesMissing()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["Missing1", "ProjectB", "Missing2"],
            ["ProjectB"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ProjectB", result[0]);
        Assert.Equal("ProjectA", result[1]);
    }

    [Fact]
    public void Sort_ShouldHandleMixedExistingAndMissing_WhenPartialDependencies()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectB", "ExternalPackage"],
            ["ProjectB"] = ["ProjectC", "AnotherExternal"],
            ["ProjectC"] = []
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("ProjectC", result[0]);
        Assert.Equal("ProjectB", result[1]);
        Assert.Equal("ProjectA", result[2]);
    }

    // ============================================
    // Order Stability Tests
    // ============================================

    [Fact]
    public void Sort_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        // Arrange
        var projects = new Dictionary<string, string[]>
        {
            ["A"] = ["B"],
            ["B"] = ["C"],
            ["C"] = []
        };

        // Act
        var result1 = TopologicalSorter.Sort(projects);
        var result2 = TopologicalSorter.Sort(projects);
        var result3 = TopologicalSorter.Sort(projects);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    // ============================================
    // Self-Reference Tests
    // ============================================

    [Fact]
    public void Sort_ShouldHandleSelfReference_WhenProjectDependsOnItself()
    {
        // Arrange - This shouldn't happen in practice but test defensive behavior
        var projects = new Dictionary<string, string[]>
        {
            ["ProjectA"] = ["ProjectA"]
        };

        // Act
        var result = TopologicalSorter.Sort(projects);

        // Assert - Should still return the project (visited flag prevents infinite loop)
        Assert.Single(result);
        Assert.Equal("ProjectA", result[0]);
    }
}
