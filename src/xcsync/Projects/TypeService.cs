using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Serilog;

namespace xcsync.Projects;

// Type system bridge between .NET and Xcode
class TypeService : ITypeService {
	static ILogger? Logger { get; set; }

	// Init
	readonly ConcurrentDictionary<string, TypeMapping> clrTypes = new ();
	readonly ConcurrentDictionary<string, TypeMapping> objCTypes = new ();

	// Add, register new type mapping
	public TypeMapping? AddType (TypeMapping newType)
	{
		if (!clrTypes.TryAdd (newType.ClrType, newType)) {
			Logger?.Error (Strings.TypeService.DuplicateType (newType.ClrType), newType.ClrType);
			return null;
		}

		if (!objCTypes.TryAdd (newType.ObjCType, newType)) {
			clrTypes.TryRemove (newType.ClrType, out _);
			Logger?.Error (Strings.TypeService.DuplicateType (newType.ObjCType), newType.ObjCType);
			return null;
		}

		return newType;
	}

	public TypeMapping? AddType (INamedTypeSymbol typeSymbol, string clrType, string objCType, TypeMapping? baseType, bool isModel, bool isProtocol,
		bool inDesigner, List<IBOutlet>? outlets, List<IBAction>? actions, HashSet<string> references)
	{
		var newType = new TypeMapping (typeSymbol, clrType, objCType, baseType, isModel, isProtocol, inDesigner, outlets, actions, references);
		return AddType (newType);
	}

	public virtual IEnumerable<TypeMapping> QueryTypes (string? clrType = null, string? objcType = null) =>
		(clrType, objcType) switch {
			(string cli, null) => clrTypes.Values.Where (t => t.ClrType == cli),
			(null, string objc) => objCTypes.Values.Where (t => t.ObjCType == objc),
			(string clr, string objc) => clrTypes.Values.Where (t => t.ClrType == clr && t.ObjCType == objc),
			_ => clrTypes.Values
		};
}
