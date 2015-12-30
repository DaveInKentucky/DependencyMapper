using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DependencyMapper
{
    public static class DependencyMapper
    {


        public static List<VsItem> GetItems(string rootPath)
        {
            var snp = FindSolutionsAndProjects(rootPath).ToList();
            snp.AddRange(PopulateDependencyOf(snp));
            return snp;
        }

        private static IEnumerable<VsItem> FindSolutionsAndProjects(string rootPath)
        {
            var dirsToCheck = new Queue<string>();
            dirsToCheck.Enqueue(rootPath);

            while (dirsToCheck.Any())
            {
                var dir = dirsToCheck.Dequeue();

                foreach (var filename in Directory.EnumerateFiles(dir))
                {
                    if (IsSolution(filename))
                    {
                        var item = new VsItem(Path.GetFileName(filename), filename, VsItemType.Solution);
                        item.Dependencies.AddRange(FindSolutionDependencies(item.Path));
                        yield return item;
                    }
                    else if (IsProject(filename))
                    {
                        var item = new VsItem(Path.GetFileName(filename), filename, VsItemType.Project);
                        item.Dependencies.AddRange(FindProjectDependencies(item.Path));
                        yield return item;
                    }
                    // Don't add a call to IsDll() here.  Doing so will include a LOT of irrelevant DLLs.  DLLs that are
                    // actually called out are added in PopulateDependencyOf().
                }

                foreach (var subDir in Directory.GetDirectories(dir))
                    dirsToCheck.Enqueue(subDir);
            }
        }

        private static IEnumerable<VsItem> PopulateDependencyOf(List<VsItem> items)
        {
            var nonExistantItems = new List<VsItem>();
            var mapping = items.ToDictionary(i => CanonicalPath(i.Path), i => i);
            foreach (var item in items)
            {
                foreach (var dep in item.Dependencies)
                {
                    var depCanPath = CanonicalPath(dep.Path);
                    if (!mapping.ContainsKey(depCanPath))
                    {
                        var type = IsDll(dep.Path) ? VsItemType.Dll : VsItemType.Project;
                        var newItem = new VsItem(Path.GetFileName(dep.Path), dep.Path, type, dep.Exists);
                        mapping[depCanPath] = newItem;

                        if (!dep.Exists || IsDll(dep.Path))
                            nonExistantItems.Add(newItem);
                    }
                    mapping[depCanPath].DependencyOf.Add(new FilePath(item.Path));
                }
            }
            return nonExistantItems;
        }

        private static IEnumerable<FilePath> FindSolutionDependencies(string solutionPath)
        {
            var directory = Path.GetDirectoryName(solutionPath);
            using (var sr = new StreamReader(solutionPath))
            {
                for (var line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    if (!line.StartsWith("Project"))
                        continue;
                    var lineParts = line.Split(new[] { ", " }, StringSplitOptions.None);
                    var projPath = lineParts[1].Substring(1, lineParts[1].Length - 2);
                    if (IsProject(projPath))
                    {
                        var path = Path.GetFullPath(Path.Combine(directory, projPath));
                        yield return new FilePath(path, File.Exists(path));
                    }
                }
            }
        }

        private static IEnumerable<FilePath> FindProjectDependencies(string projectPath)
        {
            var directory = Path.GetDirectoryName(projectPath);
            using (var sr = new StreamReader(projectPath))
            {
                var fileContents = sr.ReadToEnd();
                foreach (Match match in Regex.Matches(fileContents, @"<ProjectReference Include=""(.+?)"""))
                {
                    var asdf = match.Groups[1].Value;
                    if (!IsProject(asdf))
                        continue;
                    var path = Path.GetFullPath(Path.Combine(directory, asdf));
                    yield return new FilePath(path, File.Exists(path));
                }
                foreach (Match match in Regex.Matches(fileContents, "<HintPath>(.+?)</HintPath>"))
                {
                    var asdf = match.Groups[1].Value;
                    if (!IsDll(asdf) || asdf.Contains(@"\packages\"))
                        continue;
                    var path = Path.GetFullPath(Path.Combine(directory, asdf));
                    yield return new FilePath(path, File.Exists(path));
                }

            }
        }

        private static bool IsSolution(string path)
        {
            return path.EndsWith(".sln");
        }

        private static bool IsProject(string path)
        {
            var projectExtensions = new[] { ".csproj", ".vbproj" };
            return projectExtensions.Any(path.EndsWith);
        }

        private static bool IsDll(string path)
        {
            return path.EndsWith(".dll");
        }

        private static string CanonicalPath(string path)
        {
            return path.ToLowerInvariant();
        }
    }
}
