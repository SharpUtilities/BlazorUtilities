using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.ProjectModel;

namespace Build.Helpers;

internal static class TopologicalSorter
{
    public static List<Project> Sort(List<Project> projects, Func<Project, string[]> getDependencies)
    {
        var result = new List<Project>();
        var visited = new HashSet<string>();
        var projectDict = projects.ToDictionary(p => p.Name);

        foreach (var project in projects)
        {
            Visit(project.Name);
        }

        return result;

        void Visit(string name)
        {
            if (visited.Contains(name) || !projectDict.TryGetValue(name, out var project))
            {
                return;
            }

            visited.Add(name);
            foreach (var dep in getDependencies(project))
            {
                Visit(dep);
            }

            result.Add(project);
        }
    }

    public static List<string> Sort(Dictionary<string, string[]> projects)
    {
        var result = new List<string>();
        var visited = new HashSet<string>();

        foreach (var name in projects.Keys)
        {
            Visit(name);
        }

        return result;

        void Visit(string name)
        {
            if (visited.Contains(name) || !projects.TryGetValue(name, out var value))
            {
                return;
            }

            visited.Add(name);

            foreach (var dep in value)
            {
                Visit(dep);
            }

            result.Add(name);
        }
    }
}
