// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Serilog;

namespace xcsync.Projects;

// Type system bridge between .NET and Xcode
class TypeService (ILogger Logger) : ITypeService {
	static IFileSystem FileSystem { get; set; } = new FileSystem ();

	// Init
	readonly ConcurrentDictionary<string, TypeMapping?> clrTypes = new ();
	readonly ConcurrentDictionary<string, TypeMapping> objCTypes = new ();

	public readonly ConcurrentDictionary<string, Compilation> compilations = new ();

	// Add, register new type mapping
	public TypeMapping? AddType (TypeMapping newType)
	{
		if (!clrTypes.TryAdd (newType.ClrType, newType)) {
			Logger.Error (Strings.TypeService.DuplicateType (newType.ClrType), newType.ClrType);
			return null;
		}

		if (!objCTypes.TryAdd (newType.ObjCType, newType)) {
			clrTypes.TryRemove (newType.ClrType, out _);
			Logger.Error (Strings.TypeService.DuplicateType (newType.ObjCType), newType.ObjCType);
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

	public async Task<bool> TryUpdateMappingAsync (TypeMapping oldMapping, SyntaxNode newSyntax)
	{
		var compilation = await UpdateCompilation (oldMapping.TypeSymbol!, newSyntax).ConfigureAwait (false);

		TypeMapping? newMapping = UpdateMappingFromCompilation (oldMapping, compilation);

		if (newMapping is null) {
			Logger.Error (Strings.TypeService.TypeNotFound (oldMapping.ClrType));
			return false;
		}

		if (oldMapping.ClrType != newMapping.ClrType || oldMapping.ObjCType != newMapping.ObjCType) {
			Logger.Error (Strings.TypeService.MappingMismatch (oldMapping.ClrType, oldMapping.ObjCType, newMapping.ClrType, newMapping.ObjCType));
			return false;
		}
		bool success = clrTypes.TryGetValue (oldMapping.ClrType, out var existingClrMapping);
		success &= objCTypes.TryGetValue (oldMapping.ObjCType, out var existingObjCMapping);

		if (!success || existingClrMapping is null || existingObjCMapping is null) {
			Logger.Error (Strings.TypeService.MappingNotFound (oldMapping.ClrType, oldMapping.ObjCType));
			return false;
		}

		if (clrTypes.TryUpdate (oldMapping.ClrType, newMapping, existingClrMapping)) {
			if (!objCTypes.TryUpdate (oldMapping.ObjCType, newMapping, existingObjCMapping)) {
				Logger.Error (Strings.TypeService.MappingUpdateFailed (oldMapping.ClrType, oldMapping.ObjCType));
				clrTypes.TryUpdate (oldMapping.ClrType, existingClrMapping, newMapping);
				return false;
			}
		};
		return true;
	}

	TypeMapping? UpdateMappingFromCompilation (TypeMapping oldMapping, Compilation compilation)
	{
		var namespaces = compilation!.GlobalNamespace.GetNamespaceMembers ()
			.Where (ns => ns.GetMembers ()
				.Any (member => member.Locations
					.Any (location => location.IsInSource)));

		var newMapping = compilation.GlobalNamespace.GetNamespaceMembers ()
			.Where (ns => ns.GetMembers ()
				.Any (member => member.Locations
					.Any (location => location.IsInSource)))
			.SelectMany (ns => ns.GetTypeMembers ())
			.Where (type => xcSync.IsNsoDerived (type) && type.Name == oldMapping.ClrType)
			.Select (type => oldMapping with { TypeSymbol = type, HasChanges = true })
			.FirstOrDefault ();

		return newMapping;
	}

	public virtual IEnumerable<TypeMapping?> QueryTypes (string? clrType = null, string? objcType = null) =>
		(clrType, objcType) switch {
			(string cli, null) => clrTypes.Values.Where (t => t?.ClrType == cli),
			(null, string objc) => objCTypes.Values.Where (t => t?.ObjCType == objc),
			(string clr, string objc) => clrTypes.Values.Where (t => t?.ClrType == clr && t.ObjCType == objc),
			_ => clrTypes.Values
		};

	public void AddCompilation (string targetPlatform, Compilation compilation)
	{
		if (compilation.AssemblyName is null) {
			Logger.Error (Strings.TypeService.MissingAssemblyName);
			return;
		}

		if (!compilations.TryAdd (compilation.AssemblyName, compilation)) {
			Logger.Error (Strings.TypeService.DuplicateCompilation (compilation.AssemblyName), compilation.AssemblyName);
		};

		AddTypesFromCompilation (targetPlatform, compilation);
	}

	async Task<Compilation> UpdateCompilation (INamedTypeSymbol typeSymbol, SyntaxNode newSyntax)
	{
		if (!compilations.TryGetValue (typeSymbol.ContainingAssembly.Name, out var compilation)) {
			Logger.Error (Strings.TypeService.CompilationNotFound (typeSymbol.ContainingAssembly.Name));
			throw new InvalidOperationException (Strings.TypeService.CompilationNotFound (typeSymbol.ContainingAssembly.Name));
		}

		var root = await compilation.SyntaxTrees.First (st => st.FilePath == newSyntax.SyntaxTree.FilePath).GetRootAsync ();
		if (root is null) {
			Logger.Error (Strings.TypeService.SyntaxRootNotFound (typeSymbol.Name));
			return compilation;
		}

		var newModel = compilation.ReplaceSyntaxTree (root.SyntaxTree, newSyntax.SyntaxTree);

		if (newModel.GetDiagnostics ().Any (diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) {
			Logger.Verbose (Strings.TypeService.CompilationErrorsFound (newModel.AssemblyName!));
			foreach (var diagnostic in newModel.GetDiagnostics ()) {
				if (diagnostic.Severity == DiagnosticSeverity.Error) {
					Logger.Verbose (Strings.TypeService.AssemblyDiagnosticError (newModel.AssemblyName!, diagnostic.ToString ()));
				}
			}
		}

		if (newModel.AssemblyName is null) {
			Logger.Error (Strings.TypeService.MissingAssemblyName);
			return compilation;
		}

		if (!compilations.TryUpdate (newModel.AssemblyName, newModel, compilation))
			Logger.Error (Strings.TypeService.AssemblyUpdateError (compilation.AssemblyName!));

		return newModel;
	}

	void AddTypesFromCompilation (string targetPlatform, Compilation compilation)
	{
		// limit scope of namespaces to project, do not include referenced assemblies, etc.
		var namespaces = compilation.GlobalNamespace.GetNamespaceMembers ()
			.Where (ns => ns.GetMembers ()
				.Any (member => member.Locations
					.Any (location => location.IsInSource)));

		if (namespaces is null)
			return;

		foreach (var ns in namespaces) {
			foreach (var type in ns.GetTypeMembers ().Where (xcSync.IsNsoDerived)) {
				var nsType = ConvertToTypeMapping (targetPlatform, type);
				if (nsType is not null) {
					AddType (nsType with { IsInSource = true });
				}
			}
		}
	}

	TypeMapping? ConvertToTypeMapping (string targetPlatform, INamedTypeSymbol type)
	{
		// TypeMapping bridges the gap between the .net + objc worlds
		// extracts the necessary info from the roslyn detected type for objc .h and .m file generation
		// so this method is orchestrating the conversion of the roslyn type to TypeMapping

		// If the type is not a model, we handle the properties and methods for IBOutlets and IBActions handling too
		var isModel = false;
		var isProtocol = false;
		var clrName = type.MetadataName;
		var objCName = type.Name;
		HashSet<string> refs = [];

		if (clrTypes.TryGetValue (clrName, out var existingTypeMapping))
			return existingTypeMapping;

		var baseType = type.BaseType;
		if (baseType is null) {
			clrTypes [clrName] = null; // this saves computing the type multiple times and failing each type
			return null;
		}

		var baseTypeMapping = ConvertToTypeMapping (targetPlatform, baseType);

		foreach (var a in type.GetAttributes ()) {
			switch (a.AttributeClass?.Name) {
			case "ProtocolAttribute":
				isProtocol = true;
				break;
			case "ModelAttribute":
				isModel = true;
				break;
			case "RegisterAttribute":
				objCName = a.ConstructorArguments.Length > 0
					? GetName (a) ?? objCName
					: objCName;
				break;
			}
		}

		List<IBOutlet> outlets = [];
		List<IBAction> actions = [];

		if (!isModel) {

			foreach (var property in type.GetMembers ().OfType<IPropertySymbol> ()) {

				var outletAttribute = GetAttribute (property, "OutletAttribute");

				if (outletAttribute is null)
					continue;

				outlets.Add (new IBOutlet (
					clrName: property.Name,
					objcName: GetName (outletAttribute) ?? property.Name,
					clrType: property.Type.MetadataName,
					objcType: GetObjCType (targetPlatform, (INamedTypeSymbol) property.Type),
					isCollection: property.Type.TypeKind == TypeKind.Array));

				refs.Add (property.ContainingNamespace.Name);
			}

			foreach (var method in type.GetMembers ().OfType<IMethodSymbol> ()) {

				var actionAttribute = GetAttribute (method, "ActionAttribute");

				if (actionAttribute is null)
					continue;

				var actionName = GetName (actionAttribute);
				(string? objcName, string []? strings) = GetInfo (method, actionName);

				List<IBActionParameter> parameters = new (method.Parameters.Length);

				var index = 0;
				foreach (var param in method.Parameters) {
					parameters.Add (new IBActionParameter (param.Name, strings? [index].Length == 0 ? null : strings? [index], param.Type.MetadataName, GetObjCType (targetPlatform, (INamedTypeSymbol) param.Type)));
					index++;
					refs.Add (param.ContainingNamespace.Name);
				}

				actions.Add (new IBAction (method.Name, objcName, parameters));
			}
		}

		refs.Add (type.ContainingNamespace.Name);

		if (baseTypeMapping is not null)
			refs.UnionWith (baseTypeMapping.References);

		var typeMapping = new TypeMapping (type, clrName, objCName, baseTypeMapping, isModel, isProtocol, InDesignerFile (type, objCName),
			outlets.Count == 0 ? null : outlets, actions.Count == 0 ? null : actions,
			refs.Intersect (xcSync.ApplePlatforms [targetPlatform].SupportedFrameworks.Keys).ToHashSet ());

		return typeMapping;
	}

	bool InDesignerFile (ITypeSymbol type, string objCName)
	{
		var source = type.Locations.FirstOrDefault ()?.SourceTree;
		var sourceDir = FileSystem.Path.GetDirectoryName (source?.FilePath) ?? string.Empty;
		return FileSystem.File.Exists (FileSystem.Path.Combine (sourceDir, $"{objCName}.designer.cs"));
	}

	static AttributeData? GetAttribute (ISymbol symbol, string attributeName) =>
		symbol.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == attributeName);

	static string? GetName (AttributeData attribute) =>
		attribute.ConstructorArguments.FirstOrDefault ().Value?.ToString ();

	string? GetObjCType (string targetPlatform, INamedTypeSymbol symbol)
	{
		clrTypes.TryGetValue (symbol.MetadataName, out var nsType);
		return nsType?.ObjCType ?? ConvertToTypeMapping (targetPlatform, symbol)?.ObjCType;
	}

	static (string objcName, string []? @params) GetInfo (IMethodSymbol method, string? actionAttributeName)
	{
		if (actionAttributeName is null)
			return (method.Name, null);
		var split = actionAttributeName.Split (":", StringSplitOptions.TrimEntries);
		return split.Length > 1 ? (split [0], split [1..]) : (split [0], null);
	}

}
