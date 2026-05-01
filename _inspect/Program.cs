using System.Reflection;
using System.Reflection.MetadataLoadContext;
using System.Runtime.Loader;
using System.Linq;
var resolver = new PathAssemblyResolver(
    System.IO.Directory.GetFiles(@"C:\Program Files\dotnet\packs\Microsoft.iOS.Ref.net10.0_26.2\26.2.10191\ref\net10.0", "*.dll")
    .Concat(System.IO.Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")));
var ctx = new MetadataLoadContext(resolver);
var asm = ctx.LoadFromAssemblyPath(@"C:\Program Files\dotnet\packs\Microsoft.iOS.Ref.net10.0_26.2\26.2.10191\ref\net10.0\Microsoft.iOS.dll");
var t = asm.GetType("CoreBluetooth.CBPeripheralDelegate");
foreach (var m in t.GetMethods().Where(m => m.Name.Contains("Characteristic") || m.Name.Contains("Discov") || m.Name.Contains("Updated")))
    Console.WriteLine($"{m.Name}({string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
