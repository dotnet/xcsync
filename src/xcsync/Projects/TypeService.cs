using System.Collections.Concurrent;
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

	public TypeMapping? AddType (string CliType, string ObjCType, TypeMapping? BaseType, bool IsModel, bool IsProtocol,
		bool InDesigner, List<IBOutlet>? Outlets, List<IBAction>? Actions, HashSet<string> References)
	{
		var newType = new TypeMapping(CliType, ObjCType, BaseType, IsModel, IsProtocol, InDesigner, Outlets, Actions, References);
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
