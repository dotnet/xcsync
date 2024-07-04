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

		var project = await workspace.OpenProjectAsync (RootPath).ConfigureAwait (false);

		var compilation = await project.GetCompilationAsync ().ConfigureAwait (false) ??
						  throw new InvalidOperationException ("Could not get compilation for project '{project}'");

		if (!xcSync.TryGetTargetPlatform (Logger, Framework, out string targetPlatform))
			return project;

		TypeService.AddCompilation (targetPlatform, compilation);

		return project;
	}

}

