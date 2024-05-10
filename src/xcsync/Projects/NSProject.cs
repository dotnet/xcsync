// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Microsoft.CodeAnalysis;

namespace xcsync.Projects;

class NSProject (IFileSystem fileSystem, Dotnet project, string targetPlatform) {

	public Dictionary<string, NSObject?> cliTypes = [];
	public Dictionary<string, NSObject> objCTypes = [];
	public Dotnet DotnetProject { get; set; } = project;

	IFileSystem fileSystem { get; } = fileSystem;

	public async IAsyncEnumerable<NSObject> GetTypes ()
	{
		var openProject = await DotnetProject.OpenProject ().ConfigureAwait (false);
		await foreach (var type in DotnetProject.GetNsoTypes (openProject).ConfigureAwait (false)) {
			var nsType = ConvertToNSObject (type);
			if (nsType is not null && !nsType.IsProtocol)
				yield return nsType;
		}
	}
	public NSObject? ConvertToNSObject (ITypeSymbol type)
	{
		// NSObjectType bridges the gap between the .net + objc worlds
		// extracts the necessary info from the roslyn detected type for objc .h and .m file generation
		// so this method is orchestrating the conversion of the roslyn type to the NSObjectType

		// If the type is not a model, we handle the properties and methods for IBOutlets and IBActions handling too
		var isModel = false;
		var isProtocol = false;
		var cliName = type.MetadataName;
		var objCName = type.Name;
		HashSet<string> refs = [];

		if (cliTypes.TryGetValue (cliName, out var existingNSObject))
			return existingNSObject;

		var baseType = type.BaseType;
		if (baseType is null) {
			cliTypes [cliName] = null; // this saves computing the type multiple times and failing each type
			return null;
		}

		var baseNSObject = ConvertToNSObject (baseType);

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
					? a.GetName () ?? objCName
					: objCName;
				break;
			}
		}

		List<IBOutlet> outlets = [];
		List<IBAction> actions = [];

		if (!isModel) {

			foreach (var property in type.GetMembers ().OfType<IPropertySymbol> ()) {

				var outletAttribute = property.GetAttribute ("OutletAttribute");

				if (outletAttribute is null)
					continue;

				outlets.Add (new IBOutlet (
					cliName: property.Name,
					objcName: outletAttribute.GetName () ?? property.Name,
					cliType: property.Type.MetadataName,
					objcType: property.Type.GetObjCType (this),
					isCollection: property.Type.TypeKind == TypeKind.Array));

				refs.Add (property.ContainingNamespace.Name);
			}

			foreach (var method in type.GetMembers ().OfType<IMethodSymbol> ()) {

				var actionAttribute = method.GetAttribute ("ActionAttribute");

				if (actionAttribute is null)
					continue;

				var actionName = actionAttribute.GetName ();
				(string? objcName, string []? strings) = method.GetInfo (actionName);

				List<IBActionParameter> parameters = new (method.Parameters.Length);

				var index = 0;
				foreach (var param in method.Parameters) {
					parameters.Add (new IBActionParameter (param.Name, strings? [index].Length == 0 ? null : strings? [index], param.Type.MetadataName, param.Type.GetObjCType (this)));
					index++;
					refs.Add (param.ContainingNamespace.Name);
				}

				actions.Add (new IBAction (method.Name, objcName, parameters));
			}
		}

		refs.Add (type.ContainingNamespace.Name);

		if (baseNSObject is not null)
			refs.UnionWith (baseNSObject.References);

		var nsObject = new NSObject (cliName, objCName, baseNSObject, isModel, isProtocol, InDesignerFile (type, objCName),
			outlets.Count == 0 ? null : outlets, actions.Count == 0 ? null : actions,
			refs.Intersect (xcSync.ApplePlatforms [targetPlatform].SupportedFrameworks.Keys).ToHashSet ());

		cliTypes.Add (nsObject.CliType, nsObject);
		objCTypes.Add (nsObject.ObjCType, nsObject);

		return nsObject;
	}

	public bool InDesignerFile (ITypeSymbol type, string objCName)
	{
		var source = type.Locations.FirstOrDefault ()?.SourceTree;
		var sourceDir = fileSystem.Path.GetDirectoryName (source?.FilePath) ?? string.Empty;
		return fileSystem.File.Exists (fileSystem.Path.Combine (sourceDir, $"{objCName}.designer.cs"));
	}

}

static class Extensions {

	public static AttributeData? GetAttribute (this ISymbol symbol, string attributeName) =>
		symbol.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == attributeName);

	public static string? GetName (this AttributeData attribute) =>
		attribute.ConstructorArguments.FirstOrDefault ().Value?.ToString ();

	public static string? GetObjCType (this ITypeSymbol symbol, NSProject nsProject)
	{
		nsProject.cliTypes.TryGetValue (symbol.MetadataName, out var nsType);
		return nsType?.ObjCType ?? nsProject.ConvertToNSObject (symbol)?.ObjCType;
	}

	public static (string objcName, string []? @params) GetInfo (this IMethodSymbol method, string? actionAttributeName)
	{
		if (actionAttributeName is null)
			return (method.Name, null);
		var split = actionAttributeName.Split (":", StringSplitOptions.TrimEntries);
		return split.Length > 1 ? (split [0], split [1..]) : (split [0], null);
	}
}
