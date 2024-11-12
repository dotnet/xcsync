// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Serilog;

namespace xcsync.Projects;

class ClrProject (IFileSystem fileSystem, ILogger logger, ITypeService typeService, string name, string projectPath, string framework)
	: SyncableProject (fileSystem, logger, typeService, name, projectPath, framework, new ExtensionFilter (".cs", ".csproj", ".sln")) {

	public bool IsMauiApp { get; private set; }

	public async Task<Project> OpenProject ()
	{

		using var workspace = MSBuildWorkspace.Create (new Dictionary<string, string> {
			{"TargetFrameworks", Framework}
		});

		var project = await workspace.OpenProjectAsync (RootPath).ConfigureAwait (false);

		try {
			var compilation = await project.GetCompilationAsync ().ConfigureAwait (false)
				?? throw new NotSupportedException (Strings.ClrProject.NotSupportedException (project.Name));

			var errors = compilation.GetParseDiagnostics ().Where (d => d.Severity == DiagnosticSeverity.Error);
			if (errors?.Any () == true) {
				var errorMessages = string.Join (Environment.NewLine, errors.Select (e => e.ToString ()));
				throw new InvalidOperationException (Strings.ClrProject.InvalidOperationException (project.Name, errorMessages));
			}

			if (!xcSync.TryGetTargetPlatform (Logger, Framework, out string targetPlatform))
				return project;

			IsMauiApp = Scripts.IsMauiAppProject (RootPath);

			TypeService.AddCompilation (targetPlatform, compilation);
		} catch (InvalidOperationException ex) {
			Logger.Error (ex, Strings.ClrProject.CompilationError (project.Name, ex.Message));
		} catch (NotSupportedException ex) {
			Logger.Error (ex, Strings.ClrProject.InvalidOperationError (project.Name, ex.Message));
		} catch (Exception ex) {
			Logger.Error (ex, Strings.ClrProject.UnexpectedError (project.Name, ex.Message));
		}

		return project;
	}

}

