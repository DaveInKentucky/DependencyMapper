using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DependencyMapper
{
    public static class ReportMaker
    {
        public static void Generate(string rootDir, List<VsItem> items, string outputPath)
        {
            var slnTemplate = new Template(File.ReadAllText("Templates/Solution.html"));
            var slns = new StringBuilder();
            var numSlns = 0;

            var prjTemplate = new Template(File.ReadAllText("Templates/Project.html"));
            var prjs = new StringBuilder();
            var numPrjs = 0;

            var dllTemplate = new Template(File.ReadAllText("Templates/Dll.html"));
            var dlls = new StringBuilder();
            var numDlls = 0;

            var toc = new StringBuilder();

            foreach (var item in items.OrderBy(i => i.Name))
            {
                var className = GetClassName(item);

                toc.AppendFormat("<li><a href=\"#{1}\" class=\"{2}\" title=\"{3}\">{0}</a></li>\n", item.Name, MakeId(item.Path), className, item.Path);

                var replacements = new Dictionary<string, string>
                {
                    {"path", MakeId(item.Path)},
                    {"name", item.Name},
                    {"class", className},
                    {"relativepath", RelativePath(rootDir, item.Path)},
                    {"dependencies", MakeHtmlList(rootDir, item.Dependencies)},
                    {"dependedonby", MakeHtmlList(rootDir, item.DependencyOf)}
                };

                switch (item.Type)
                {
                    case VsItemType.Solution:
                        numSlns += 1;
                        slns.Append(slnTemplate.Fill(replacements));
                        break;
                    case VsItemType.Project:
                        numPrjs += 1;
                        prjs.Append(prjTemplate.Fill(replacements));
                        break;
                    case VsItemType.Dll:
                        numDlls += 1;
                        dlls.Append(dllTemplate.Fill(replacements));
                        break;
                }
            }

            var pageTemplate = new Template(File.ReadAllText("Templates/Page.html"));
            File.WriteAllText(outputPath, pageTemplate.Fill(new Dictionary<string, string>
            {
                {"rootdir", rootDir},
                {"numsolutions", numSlns.ToString()},
                {"numprojects", numPrjs.ToString()},
                {"numdlls", numDlls.ToString()},
                {"toc", toc.ToString()},
                {"solutions", slns.ToString()},
                {"projects", prjs.ToString()},
                {"dlls", dlls.ToString()},
            }));
        }

        private static string MakeHtmlList(string rootDir, List<FilePath> asdf)
        {
            if (!asdf.Any())
                return "None";

            var sb = new StringBuilder("<ul>\n");
            foreach (var dep in asdf.OrderBy(s => s.Path))
                sb.AppendFormat("<li><a href=\"#{1}\" class=\"{2}\"><code>{0}</code></a></li>\n", RelativePath(rootDir, dep.Path), MakeId(dep.Path), GetClassName(dep));
            sb.Append("</ul>\n");
            return sb.ToString();
        }

        private static string MakeId(string x)
        {
            return x.Replace('\\', '-').ToLowerInvariant();
        }

        private static string RelativePath(string dir, string path)
        {
            if (!path.StartsWith(dir))
                return path;
            return path.Substring(dir.Length + 1);
        }

        private static string GetClassName(VsItem item)
        {
            var directDllPaths = new[] { @"\bin\Debug\", @"\bin\Release\" };
            if (item.Type == VsItemType.Dll && directDllPaths.Any(item.Path.Contains))
                return "direct-dll";

            if (!item.Exists)
                return "nonexistant";

            if (item.Type != VsItemType.Solution && item.IsOrphan)
                return "orphan";

            var hasNonexistantDeps = item.Dependencies.Any(d => !d.Exists);
            if (hasNonexistantDeps)
                return "nonexistant-deps";

            return String.Empty;
        }

        private static string GetClassName(FilePath path)
        {
            if (!path.Exists)
                return "nonexistant";

            return String.Empty;
        }
    }
}
