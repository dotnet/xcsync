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
			foreach (var type in ns.GetTypeMembers ()) {
				yield return type;
			}
		}
	}
}
