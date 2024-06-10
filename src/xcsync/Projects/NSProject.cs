// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Microsoft.CodeAnalysis;

namespace xcsync.Projects;

class NSProject (IFileSystem fileSystem, ClrProject project, string targetPlatform) {

	public Dictionary<string, TypeMapping?> clrTypes = [];
	public Dictionary<string, TypeMapping> objCTypes = [];
	public ClrProject ClrProject { get; set; } = project;

	IFileSystem fileSystem { get; } = fileSystem;

	public async IAsyncEnumerable<TypeMapping> GetTypes ()
	{
		var openProject = await ClrProject.OpenProject ().ConfigureAwait (false);
		await foreach (var type in ClrProject.GetNsoTypes (openProject).ConfigureAwait (false)) {
			var nsType = ConvertToTypeMapping (type);
			if (nsType is not null && !nsType.IsProtocol) {
				// todo: use context to add Type to the TypeService
				yield return nsType;
			}
		}
	}
	public TypeMapping? ConvertToTypeMapping (INamedTypeSymbol type)
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

		var baseTypeMapping = ConvertToTypeMapping (baseType);

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
					objcType: GetObjCType ((property.Type as INamedTypeSymbol)!, this),
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
					parameters.Add (new IBActionParameter (param.Name, strings? [index].Length == 0 ? null : strings? [index], param.Type.MetadataName, GetObjCType ((param.Type as INamedTypeSymbol)!, this)));
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

		clrTypes.Add (typeMapping.ClrType, typeMapping);
		objCTypes.Add (typeMapping.ObjCType, typeMapping);

		return typeMapping;
	}

	bool InDesignerFile (ITypeSymbol type, string objCName)
	{
		var source = type.Locations.FirstOrDefault ()?.SourceTree;
		var sourceDir = fileSystem.Path.GetDirectoryName (source?.FilePath) ?? string.Empty;
		return fileSystem.File.Exists (fileSystem.Path.Combine (sourceDir, $"{objCName}.designer.cs"));
	}

	static AttributeData? GetAttribute (ISymbol symbol, string attributeName) =>
		symbol.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == attributeName);

	static string? GetName (AttributeData attribute) =>
		attribute.ConstructorArguments.FirstOrDefault ().Value?.ToString ();

	static string? GetObjCType (INamedTypeSymbol symbol, NSProject nsProject)
	{
		nsProject.clrTypes.TryGetValue (symbol.MetadataName, out var nsType);
		return nsType?.ObjCType ?? nsProject.ConvertToTypeMapping (symbol)?.ObjCType;
	}

	static (string objcName, string []? @params) GetInfo (IMethodSymbol method, string? actionAttributeName)
	{
		if (actionAttributeName is null)
			return (method.Name, null);
		var split = actionAttributeName.Split (":", StringSplitOptions.TrimEntries);
		return split.Length > 1 ? (split [0], split [1..]) : (split [0], null);
	}
}
