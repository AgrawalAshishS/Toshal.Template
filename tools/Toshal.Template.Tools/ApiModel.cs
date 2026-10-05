using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace Toshal.Template.Tools;

/// <summary>Finds the public API by reflection and pairs each member with its XML comment.</summary>
internal sealed class ApiModel
{
    public record MemberInfoEx(string Kind, string Signature, string Id, XElement? Doc, bool Obsolete);
    public record TypeInfoEx(Type Type, string Name, string FileName, string Kind, XElement? Doc, bool Obsolete, List<MemberInfoEx> Members);

    private readonly Dictionary<string, XElement> docs = new();

    public List<TypeInfoEx> Types { get; } = new();

    public ApiModel(IEnumerable<Assembly> assemblies)
    {
        foreach (var asm in assemblies)
        {
            string xml = Path.ChangeExtension(asm.Location, ".xml");
            if (!File.Exists(xml)) throw new FileNotFoundException("XML doc file not found. Build the library first.", xml);
            foreach (var m in XDocument.Load(xml).Descendants("member"))
            {
                docs[(string)m.Attribute("name")!] = m;
            }
        }

        foreach (var asm in assemblies)
        {
            foreach (var t in asm.GetExportedTypes().OrderBy(t => t.Namespace).ThenBy(t => t.FullName))
            {
                Types.Add(Describe(t));
            }
        }
    }

    public bool TryGetTypePage(string docId, out string fileName)
    {
        var t = Types.FirstOrDefault(x => TypeId(x.Type) == docId);
        fileName = t?.FileName ?? "";
        return t != null;
    }

    private TypeInfoEx Describe(Type t)
    {
        string kind = t.IsInterface ? "interface" : t.IsAbstract && t.IsSealed ? "static class" : t.IsAbstract ? "abstract class" : "class";
        var members = new List<MemberInfoEx>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var c in t.GetConstructors(flags).Where(IsVisible).OrderBy(c => c.GetParameters().Length))
        {
            // A constructor the compiler adds has no comment to show.
            if (c.GetParameters().Length == 0 && Doc(MethodId(c)) == null) continue;
            members.Add(new("Constructor", $"{ShortName(t)}({Params(c)})", MethodId(c), Doc(MethodId(c)), IsObsolete(c)));
        }
        foreach (var p in t.GetProperties(flags).Where(p => (p.GetMethod != null && IsVisible(p.GetMethod)) || (p.SetMethod != null && IsVisible(p.SetMethod))).OrderBy(p => p.Name))
        {
            string get = p.GetMethod != null && IsVisible(p.GetMethod) ? " get;" : "";
            string set = p.SetMethod != null && IsVisible(p.SetMethod) ? " set;" : "";
            string id = PropertyId(p);
            string name = p.GetIndexParameters().Length > 0 ? $"this[{Params(p.GetIndexParameters())}]" : p.Name;
            members.Add(new("Property", $"{Nice(p.PropertyType)} {name} {{{get}{set} }}", id, Doc(id), IsObsolete(p)));
        }
        foreach (var f in t.GetFields(flags).Where(f => (f.IsPublic || f.IsFamily) && !f.IsSpecialName).OrderBy(f => f.Name))
        {
            string id = "F:" + TypeName(t) + "." + f.Name;
            members.Add(new("Field", $"{(f.IsStatic ? "static " : "")}{(f.IsInitOnly ? "readonly " : "")}{Nice(f.FieldType)} {f.Name}", id, Doc(id), IsObsolete(f)));
        }
        foreach (var m in t.GetMethods(flags).Where(m => IsVisible(m) && !m.IsSpecialName).OrderBy(m => m.Name).ThenBy(m => m.GetParameters().Length))
        {
            string generic = m.IsGenericMethod ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";
            string mods = m.IsStatic ? "static " : "";
            members.Add(new("Method", $"{mods}{Nice(m.ReturnType)} {m.Name}{generic}({Params(m)})", MethodId(m), Doc(MethodId(m)), IsObsolete(m)));
        }

        string file = ShortName(t, forFile: true);
        return new(t, Nice(t), file, kind, Doc(TypeId(t)), IsObsolete(t), members);
    }

    private XElement? Doc(string id) => docs.TryGetValue(id, out var e) ? e : null;
    private static bool IsObsolete(MemberInfo m) => m.GetCustomAttribute<ObsoleteAttribute>() != null;
    private static bool IsVisible(MethodBase m) => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && m.GetCustomAttribute<CompilerGeneratedAttribute>() == null;

    // ---- doc ids (see the C# XML documentation ID string format)

    public static string TypeId(Type t) => "T:" + TypeName(t);

    private static string TypeName(Type t) => (t.DeclaringType != null ? TypeName(t.DeclaringType) + "." : t.Namespace + ".") + t.Name;

    private static string MethodId(MethodBase m)
    {
        var sb = new StringBuilder("M:").Append(TypeName(m.DeclaringType!)).Append('.');
        sb.Append(m.IsConstructor ? "#ctor" : m.Name);
        if (m.IsGenericMethod) sb.Append("``").Append(m.GetGenericArguments().Length);
        var ps = m.GetParameters();
        if (ps.Length > 0) sb.Append('(').Append(string.Join(",", ps.Select(p => IdOf(p.ParameterType)))).Append(')');
        return sb.ToString();
    }

    private static string PropertyId(PropertyInfo p)
    {
        var ps = p.GetIndexParameters();
        return "P:" + TypeName(p.DeclaringType!) + "." + p.Name + (ps.Length > 0 ? "(" + string.Join(",", ps.Select(x => IdOf(x.ParameterType))) + ")" : "");
    }

    private static string IdOf(Type t)
    {
        if (t.IsByRef) return IdOf(t.GetElementType()!) + "@";
        if (t.IsArray) return IdOf(t.GetElementType()!) + "[]";
        if (t.IsGenericParameter) return (t.DeclaringMethod != null ? "``" : "`") + t.GenericParameterPosition;
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            string name = TypeName(def);
            name = name[..name.IndexOf('`')];
            return name + "{" + string.Join(",", t.GetGenericArguments().Select(IdOf)) + "}";
        }
        return TypeName(t);
    }

    // ---- readable names

    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(int)] = "int", [typeof(long)] = "long", [typeof(short)] = "short", [typeof(byte)] = "byte", [typeof(bool)] = "bool",
        [typeof(string)] = "string", [typeof(object)] = "object", [typeof(void)] = "void", [typeof(decimal)] = "decimal",
        [typeof(double)] = "double", [typeof(float)] = "float", [typeof(char)] = "char",
    };

    public static string Nice(Type t)
    {
        if (t.IsByRef) return Nice(t.GetElementType()!);
        if (t.IsArray) return Nice(t.GetElementType()!) + "[]";
        if (Keywords.TryGetValue(t, out var kw)) return kw;
        if (t.IsGenericParameter) return t.Name;
        if (t.IsGenericType)
        {
            string n = t.Name;
            int tick = n.IndexOf('`');
            if (tick < 0) return n; // a nested type inside a generic type
            int arity = int.Parse(n[(tick + 1)..]);
            var args = t.GetGenericArguments().Skip(t.GetGenericArguments().Length - arity);
            return n[..tick] + "<" + string.Join(", ", args.Select(Nice)) + ">";
        }
        return t.Name;
    }

    private static string ShortName(Type t, bool forFile = false)
    {
        string n = t.Name;
        int tick = n.IndexOf('`');
        if (tick >= 0) n = n[..tick];
        if (t.DeclaringType != null) n = ShortName(t.DeclaringType, forFile) + (forFile ? "." : ".") + n;
        return n;
    }

    private static string Params(MethodBase m) => Params(m.GetParameters());

    private static string Params(ParameterInfo[] ps)
    {
        return string.Join(", ", ps.Select(p =>
        {
            string s = "";
            if (p.GetCustomAttribute<ExtensionAttribute>() != null) s = "";
            if (p.Position == 0 && p.Member is MethodInfo mi && mi.GetCustomAttribute<ExtensionAttribute>() != null) s += "this ";
            if (p.GetCustomAttribute<ParamArrayAttribute>() != null) s += "params ";
            s += Nice(p.ParameterType) + " " + p.Name;
            if (p.HasDefaultValue) s += " = " + (p.DefaultValue is null ? "null" : p.DefaultValue is Enum e ? Nice(p.ParameterType) + "." + e : p.DefaultValue.ToString());
            return s;
        }));
    }
}
