// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    // The class of a template comes from its path, the way .resx files get theirs: Templates/Email/OrderConfirm.ctt in a project with the root
    // namespace MyApp is the class OrderConfirm in the namespace MyApp.Templates.Email. Chars that C# does not allow in a name become '_'.
    public static class TemplateNaming
    {
        public const string Extension = ".ctt";

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default",
            "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
            "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
            "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
            "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while",
        };

        // classNamespace / className: values given by the user (MSBuild metadata or tool switches); null or empty means "from the path".
        public static (string Namespace, string ClassName) FromPath(string? rootNamespace, string projectDirectory, string templatePath, string? classNamespace = null, string? className = null)
        {
            var name = string.IsNullOrWhiteSpace(className) ? Identifier(Path.GetFileNameWithoutExtension(templatePath)) : className!.Trim();
            if (!string.IsNullOrWhiteSpace(classNamespace))
            {
                return (classNamespace!.Trim(), name);
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(rootNamespace))
            {
                parts.Add(rootNamespace!.Trim());
            }

            parts.AddRange(FolderNames(projectDirectory, templatePath).Select(Identifier));
            return (string.Join(".", parts), name);
        }

        // The folders between the project folder and the template. None when the template is not inside the project folder.
        public static IEnumerable<string> FolderNames(string projectDirectory, string templatePath)
        {
            var root = Normalize(projectDirectory).TrimEnd('/') + "/";
            var file = Normalize(templatePath);
            if (root.Length <= 1 || !file.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<string>();
            }

            var folders = file.Substring(root.Length).Split('/');
            return folders.Take(folders.Length - 1).Where(f => f.Length > 0 && f != ".");
        }

        // A valid C# name: letters, digits and '_' stay, other chars become '_', a leading digit gets '_' before it, a keyword gets '@'.
        public static string Identifier(string text)
        {
            var sb = new StringBuilder(text.Length + 1);
            foreach (var c in text)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            if (sb.Length == 0 || char.IsDigit(sb[0]))
            {
                sb.Insert(0, '_');
            }

            var name = sb.ToString();
            return Keywords.Contains(name) ? "@" + name : name;
        }

        // Whether a whole name (a.b.c for a namespace) is valid C#, for the error a user gets for a bad ClassName or Namespace.
        public static bool IsValidName(string name, bool dotted)
        {
            var parts = dotted ? name.Split('.') : new[] { name };
            return parts.All(part =>
            {
                var p = part.StartsWith("@", StringComparison.Ordinal) ? part.Substring(1) : part;
                return p.Length > 0
                    && (char.IsLetter(p[0]) || p[0] == '_')
                    && p.All(c => char.IsLetterOrDigit(c) || c == '_')
                    && (part.StartsWith("@", StringComparison.Ordinal) || !Keywords.Contains(p));
            });
        }

        private static string Normalize(string path) => Path.GetFullPath(path).Replace('\\', '/');
    }
}
