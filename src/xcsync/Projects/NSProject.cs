// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.CodeAnalysis;

namespace xcsync.Projects;

public class NSProject (Dotnet project) {

	public Dictionary<string, NSObject?> cliTypes = new ();
	public Dictionary<string, NSObject> objCTypes = new ();
	public Dotnet DotnetProject { get; set; } = project;

	public async IAsyncEnumerable<NSObject> GetTypes ()
	{
		var openProject = await DotnetProject.OpenProject ().ConfigureAwait (false);
		await foreach (var type in DotnetProject.GetNsoTypes (openProject).ConfigureAwait (false)) {
			var nsType = ConvertToNSObject (type);
			if (nsType is not null)
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
		var cliName = type.MetadataName;
		var objCName = type.Name;

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
				return null;
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

		List<IBOutlet> outlets = new ();
		List<IBAction> actions = new ();

		if (!isModel) {

			outlets.AddRange (from property in type.GetMembers ().OfType<IPropertySymbol> ()
							  let outletAttribute = property.GetAttribute ("OutletAttribute")
							  where outletAttribute is not null
							  let objcName = outletAttribute.GetName () ?? property.Name
							  let cliType = property.Type.MetadataName
							  let isCollection = property.Type.TypeKind == TypeKind.Array
							  let objcType = property.Type.GetObjCType (this)
							  select new IBOutlet (property.Name, objcName, cliType, objcType, isCollection));

			actions.AddRange (from method in type.GetMembers ().OfType<IMethodSymbol> ()
							  let actionAttribute = method.GetAttribute ("ActionAttribute")
							  where actionAttribute is not null
							  let actionName = actionAttribute.GetName ()
							  let info = method.GetInfo (actionName)
							  let objcName = info.objcName

							  let parameters = method.Parameters
								  .Select ((p, i) => new IBActionParameter (p.Name, info.@params? [i].Length == 0 ? null : info.@params? [i], p.Type.MetadataName, p.Type.GetObjCType (this)))
								  .ToList ()

							  select new IBAction (method.Name, objcName, parameters));
		}

		var nsObject = new NSObject (cliName, objCName, baseNSObject, isModel, type.InDesigner (objCName), outlets.Count == 0 ? null : outlets, actions.Count == 0 ? null : actions);

		cliTypes.Add (nsObject.CliType, nsObject);
		objCTypes.Add (nsObject.ObjCType, nsObject);

		return nsObject;
	}
}

public static class Extensions {
	public static bool InDesigner (this ITypeSymbol type, string objCName)
	{
		var source = type.Locations.FirstOrDefault ()?.SourceTree;
		var sourceDir = Path.GetDirectoryName (source?.FilePath) ?? string.Empty;
		return File.Exists (Path.Combine (sourceDir, $"{objCName}.designer.cs"));
	}

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


