using System.Collections.Generic;
using System.Linq;

namespace DependencyMapper
{
    public class VsItem
    {
        public VsItem(string name, string path, VsItemType type, bool exists = true)
        {
            Name = name;
            Path = path;
            Type = type;
            Dependencies = new List<FilePath>();
            DependencyOf = new List<FilePath>();
            Exists = exists;
        }

        public string Name { get; set; }

        public string Path { get; set; }

        public VsItemType Type { get; set; }

        public List<FilePath> Dependencies { get; private set; }

        public List<FilePath> DependencyOf { get; private set; }

        public bool Exists { get; private set; }

        public bool IsOrphan
        {
            get { return !DependencyOf.Any(); }
        }

        public override string ToString()
        {
            return Path;
        }
    }

    public struct FilePath
    {
        public FilePath(string path, bool exists = true)
            : this()
        {
            Path = path;
            Exists = exists;
        }

        public string Path { get; private set; }

        public bool Exists { get; private set; }
    }

    public enum VsItemType
    {
        Project,
        Solution,
        Dll
    }
}
