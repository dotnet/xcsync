// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Serilog;

namespace xcsync.Projects;

class ClrProject (IFileSystem fileSystem, ILogger logger, ITypeService typeService, string name, string projectPath, string framework)
	: SyncableProject (fileSystem, logger, typeService, name, projectPath, framework, ["*.cs", "*.csproj", "*.sln"]) {

	public async Task<Project> OpenProject ()
	{
		// we care about the project interactions
		// the workspace is simply a means of accessing the project
		if (!MSBuildLocator.IsRegistered)
			MSBuildLocator.RegisterDefaults ();

		using var workspace = MSBuildWorkspace.Create (new Dictionary<string, string> {
			{"TargetFrameworks", Framework}
		});

		return await workspace.OpenProjectAsync (RootPath).ConfigureAwait (false);
	}

	public async IAsyncEnumerable<INamedTypeSymbol> GetNsoTypes (Project project)
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
			foreach (var type in ns.GetTypeMembers ().Where (IsNsoDerived)) {
				yield return type;
			}
		}
	}

	static bool IsNsoDerived (INamedTypeSymbol? type)
	{
		var registerAttribute = type?.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == "RegisterAttribute");
		var skipAttribute = registerAttribute?.NamedArguments.FirstOrDefault (x => x.Key == "SkipRegistration");

		if (skipAttribute?.Value.Value is true)
			return false;

		return registerAttribute is not null;
	}
}

