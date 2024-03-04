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
	public NSObject? ConvertToNSObject (INamedTypeSymbol type)
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
					? a.ConstructorArguments [0].Value!.ToString ()!
					: objCName;
				break;
			}
		}

		var nsObject = new NSObject (cliName, objCName, baseNSObject, isModel, type.InDesigner (objCName));
		cliTypes.Add (nsObject.CliType, nsObject);
		objCTypes.Add (nsObject.ObjCType, nsObject);
		if (!isModel) {
			//todo: get methods => actions
			//todo: get properties => outlets	
		}
		return nsObject;
	}
}

public static class Extensions {
	public static bool InDesigner (this INamedTypeSymbol type, string objCName)
	{
		var source = type.Locations.FirstOrDefault ()?.SourceTree;
		var sourceDir = Path.GetDirectoryName (source?.FilePath) ?? string.Empty;
		return File.Exists (Path.Combine (sourceDir, $"{objCName}.designer.cs"));
	}
}


