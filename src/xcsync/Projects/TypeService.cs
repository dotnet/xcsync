using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Serilog;

namespace xcsync.Projects;

// Type system bridge between .NET and Xcode
class TypeService : ITypeService {
	static ILogger? Logger { get; set; }

	// Init
	readonly ConcurrentDictionary<string, TypeMapping> cliTypes = new ();
	readonly ConcurrentDictionary<string, TypeMapping> objCTypes = new ();

	// Add, register new type mapping
	public TypeMapping? AddType (TypeMapping newType)
	{
		if (!cliTypes.TryAdd (newType.CliType, newType)) {
			Logger?.Error (Strings.TypeService.DuplicateType (newType.CliType), newType.CliType);
			return null;
		}

		if (!objCTypes.TryAdd (newType.ObjCType, newType)) {
			cliTypes.TryRemove (newType.CliType, out _);
			Logger?.Error (Strings.TypeService.DuplicateType (newType.ObjCType), newType.ObjCType);
			return null;
		}

		return newType;
	}

	public TypeMapping? AddType (INamedTypeSymbol typeSymbol, string cliType, string objCType, TypeMapping? baseType, bool isModel, bool isProtocol,
		bool inDesigner, List<IBOutlet>? outlets, List<IBAction>? actions, HashSet<string> references)
	{
		var newType = new TypeMapping(typeSymbol, cliType, objCType, baseType, isModel, isProtocol, inDesigner, outlets, actions, references);
		return AddType(newType);
	}

	public IEnumerable<TypeMapping> QueryTypes (string? cliType = null, string? objcType = null) =>
		(cliType, objcType) switch {
			(string cli, null) => cliTypes.Values.Where (t => t.CliType == cli),
			(null, string objc) => objCTypes.Values.Where (t => t.ObjCType == objc),
			(string cli, string objc) => cliTypes.Values.Where (t => t.CliType == cli && t.ObjCType == objc),
			_ => cliTypes.Values
		};
}
