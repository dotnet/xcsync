// Copyright (c) Microsoft Corporation.  All rights reserved.
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace xcsync.Projects;

public class Dotnet (string project) {

	public async Task<Project> OpenProject ()
	{
		// we care about the project interactions
		// the workspace is simply a means of accessing the project
		Console.WriteLine ($"Creating workspace for '{project}'");
		MSBuildLocator.RegisterDefaults ();
		using var workspace = MSBuildWorkspace.Create ();
		return await workspace.OpenProjectAsync (project).ConfigureAwait (false);
	}

	public async IAsyncEnumerable<INamedTypeSymbol> GetTypes (Project project)
	{
		var compilation = await project.GetCompilationAsync ().ConfigureAwait (false) ??
						  throw new InvalidOperationException ("Could not get compilation for project '{project}'");

		// limit scope of namespaces to project, do not include referenced assemblies, etc.
		var namespaces = compilation.GlobalNamespace.GetNamespaceMembers ()
			.Where (ns => ns.GetMembers ()
				.Any (member => member.Locations
					.Any (location => location.IsInSource)));

		if (namespaces is null)
			yield break;

		foreach (var ns in namespaces) {
			foreach (var type in ns.GetTypeMembers ().Where (type => type.IsNsoDerived ())) {

				var regAttr = type.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == "RegisterAttribute");
				var skip = regAttr?.NamedArguments.FirstOrDefault (x => x.Key == "SkipRegistration").Value.Value as bool?;

				if (skip != true)
					yield return type;
			}
		}
	}
}

public static class DotnetExtensions {
	public static bool IsNsoDerived (this INamedTypeSymbol? type)
	{
		while (type is not null) {
			if (type.Name.Equals ("NSObject", StringComparison.Ordinal) &&
				type.ContainingNamespace.Name.Equals ("Foundation", StringComparison.Ordinal))
				return true;

			type = type.BaseType;
		}

		return false;
	}
}
