// specgen: reads the spec -- RhinoCommon's C# sources -- and generates from it (vNext).
//
//   dotnet run --project tools/specgen -- extract   [--rhino3dm] <rhino-dotnet-dir> <members.json>
//   dotnet run --project tools/specgen -- translate <rhino-dotnet-dir> <valuetypes.json>
//   dotnet run --project tools/specgen -- all       <rhino-dotnet-dir> <members.json> <valuetypes.json>
//
// extract    the public surface: signatures, native calls, body shapes
//            (feeds api/manifest.json through tools/extract/manifest.py)
// translate  value-type bodies (Point3d, Vector3d, Interval, RhinoMath) to
//            C++ (api/valuetypes.json, consumed by tools/generate/bnd_gen.py)
// all        both, from ONE parse of the ~490 sources
//
// <rhino-dotnet-dir> defaults to ~/dev/rhino/src4/DotNetSDK/rhinocommon/dotnet.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var cmd = args.Length > 0 ? args[0] : "";
var rest = args.Skip(1).ToArray();
var rhino3dmOnly = rest.Contains("--rhino3dm");
var pos = rest.Where(a => !a.StartsWith("--")).ToArray();
var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                              "dev", "rhino", "src4", "DotNetSDK", "rhinocommon", "dotnet");
var dir = pos.Length > 0 ? pos[0] : defaultDir;
if (!Directory.Exists(dir))
{
    Console.Error.WriteLine($"not a rhinocommon dotnet dir: {dir}");
    return 1;
}
var src = new Sources(dir);

switch (cmd)
{
    case "extract":
        return ExtractCommand.Run(dir, pos.Length > 1 ? pos[1] : "members.json", rhino3dmOnly, src);
    case "translate":
        return TranslateCommand.Run(pos.Length > 1 ? pos[1] : "valuetypes.json", src);
    case "all":
    {
        int rc = ExtractCommand.Run(dir, pos.Length > 1 ? pos[1] : "members.json", rhino3dmOnly, src);
        return rc != 0 ? rc : TranslateCommand.Run(pos.Length > 2 ? pos[2] : "valuetypes.json", src);
    }
    default:
        Console.Error.WriteLine("usage: specgen extract|translate|all [--rhino3dm] <rhino-dotnet-dir> <out.json> [<valuetypes.json>]");
        return 2;
}

/// <summary>
/// The RhinoCommon sources, enumerated once, each file parsed at most once per
/// preprocessor view and cached: `all` parses ~490 files twice (two views)
/// instead of three times.
/// </summary>
sealed class Sources
{
    // RHINO3DM_BUILD: the surface rhino3dm compiles. RHINO_SDK: the Rhino-only
    // surface, recorded (not filtered) because full in-Rhino bindings are a goal.
    public static readonly CSharpParseOptions Portable = new(
        documentationMode: DocumentationMode.Parse,
        preprocessorSymbols: new[] { "RHINO3DM_BUILD" });
    public static readonly CSharpParseOptions Sdk = new(
        documentationMode: DocumentationMode.Parse,
        preprocessorSymbols: new[] { "RHINO_SDK" });

    public readonly List<string> Files;
    readonly Dictionary<string, string> _text = new();
    readonly Dictionary<(string, CSharpParseOptions), SyntaxNode> _roots = new();

    public Sources(string dir)
    {
        var sep = Path.DirectorySeparatorChar;
        Files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{sep}obj{sep}") && !p.Contains($"{sep}bin{sep}"))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    public SyntaxNode Root(string file, CSharpParseOptions options)
    {
        if (!_roots.TryGetValue((file, options), out var root))
        {
            if (!_text.TryGetValue(file, out var text))
                _text[file] = text = File.ReadAllText(file);
            root = CSharpSyntaxTree.ParseText(text, options).GetRoot();
            _roots[(file, options)] = root;
        }
        return root;
    }
}
