using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DependencyMapper
{
    public class Template
    {
        private const string PlaceholderStartMarker = "{{";

        private const string PlaceholderEndMarker = "}}";

        private readonly List<IChunk> _compiled;

        public Template(string template)
        {
            _compiled = Compile(template).ToList();
        }

        private static IEnumerable<IChunk> Compile(string html)
        {
            for (var offset = 0; offset < html.Length;)
            {
                IChunk chunk;
                if (Placeholder.TryExtract(html, ref offset, out chunk))
                {
                    yield return chunk;
                    continue;
                }
                if (Literal.TryExtract(html, ref offset, out chunk))
                    yield return chunk;
            }
        }

        public string Fill(Dictionary<string, string> replacements)
        {
            return _compiled
                .Aggregate(new StringBuilder(), (sb, chunk) => sb.Append(chunk.Render(replacements)))
                .ToString();
        }

        private interface IChunk
        {
            string Render(Dictionary<string, string> context);
        }

        private class Literal : IChunk
        {
            private readonly string _literal;

            private Literal(string literal)
            {
                _literal = literal;
            }

            public string Render(Dictionary<string, string> context)
            {
                return _literal;
            }

            public static bool TryExtract(string html, ref int offset, out IChunk chunk)
            {
                var placeholderStart = html.IndexOf(PlaceholderStartMarker, offset, StringComparison.InvariantCulture);
                if (placeholderStart == offset)
                {
                    chunk = null;
                    return false;
                }
                if (placeholderStart == -1)
                {
                    chunk = new Literal(html.Substring(offset));
                    offset = html.Length;
                }
                else
                {
                    chunk = new Literal(html.Substring(offset, placeholderStart - offset));
                    offset = placeholderStart;
                }
                return true;
            }

            public override string ToString()
            {
                return _literal;
            }
        }

        private class Placeholder : IChunk
        {
            private readonly string _name;

            private Placeholder(string name)
            {
                _name = name;
            }

            public string Render(Dictionary<string, string> context)
            {
                return context[_name];
            }

            public static bool TryExtract(string html, ref int offset, out IChunk chunk)
            {
                chunk = null;
                if (!html.IsSubstringAt(PlaceholderStartMarker, offset))
                    return false;

                var nameStart = offset + PlaceholderStartMarker.Length;
                var end = html.IndexOf(PlaceholderEndMarker, nameStart, StringComparison.InvariantCulture);
                if (end == -1)
                    return false;

                var name = html.Substring(nameStart, end - nameStart);
                chunk = new Placeholder(name);
                offset = end + PlaceholderEndMarker.Length;
                return true;
            }

            public override string ToString()
            {
                return String.Concat(PlaceholderStartMarker, _name, PlaceholderEndMarker);
            }
        }
    }

    public static class StringExtensions
    {
        public static bool IsSubstringAt(this string str, string value, int offset)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(value))
                return false;
            if (str.Length < offset + value.Length)
                return false;
            for (var i = 0; i < value.Length ; i++)
                if (str[offset + i] != value[i])
                    return false;
            return true;
        }
    }
}
