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

	public bool TryUpdateMapping (TypeMapping oldMapping, TypeMapping newMapping)
	{
		if (oldMapping.ClrType != newMapping.ClrType || oldMapping.ObjCType != newMapping.ObjCType) {
			Logger?.Error (Strings.TypeService.MappingMismatch (oldMapping.ClrType, oldMapping.ObjCType, newMapping.ClrType, newMapping.ObjCType));
			return false;
		}
		bool success = clrTypes.TryGetValue (oldMapping.ClrType, out var existingClrMapping);
		success &= objCTypes.TryGetValue (oldMapping.ObjCType, out var existingObjCMapping);

		if (!success || existingClrMapping is null || existingObjCMapping is null) {
			Logger?.Error (Strings.TypeService.MappingNotFound (oldMapping.ClrType, oldMapping.ObjCType));
			return false;
		}

		if (clrTypes.TryUpdate (oldMapping.ClrType, newMapping, existingClrMapping)) {
			if (!objCTypes.TryUpdate (oldMapping.ObjCType, newMapping, existingObjCMapping)) {
				Logger?.Error (Strings.TypeService.MappingUpdateFailed (oldMapping.ClrType, oldMapping.ObjCType));
				clrTypes.TryUpdate (oldMapping.ClrType, existingClrMapping, newMapping);
				return false;
			}
		};
		return true;
	}

	public virtual IEnumerable<TypeMapping> QueryTypes (string? clrType = null, string? objcType = null) =>
		(clrType, objcType) switch {
			(string cli, null) => clrTypes.Values.Where (t => t.ClrType == cli),
			(null, string objc) => objCTypes.Values.Where (t => t.ObjCType == objc),
			(string clr, string objc) => clrTypes.Values.Where (t => t.ClrType == clr && t.ObjCType == objc),
			_ => clrTypes.Values
		};
}
