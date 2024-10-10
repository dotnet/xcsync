// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Serilog;

namespace xcsync.Projects;

class ClrProject (IFileSystem fileSystem, ILogger logger, ITypeService typeService, string name, string projectPath, string framework)
	: SyncableProject (fileSystem, logger, typeService, name, projectPath, framework, new ExtensionFilter ([".cs", ".csproj", ".sln"])) {

	public bool IsMauiApp { get; private set; }

	public async Task<Project> OpenProject ()
	{

		using var workspace = MSBuildWorkspace.Create (new Dictionary<string, string> {
			{"TargetFrameworks", Framework}
		});

		var project = await workspace.OpenProjectAsync (RootPath).ConfigureAwait (false);

		try {
			var compilation = await project.GetCompilationAsync ().ConfigureAwait (false)
				?? throw new NotSupportedException ($"Compilation not supported for current project '{project}'");

			var errors = compilation.GetParseDiagnostics ().Where (d => d.Severity == DiagnosticSeverity.Error);
			if (errors?.Any () == true) {
				var errorMessages = string.Join (Environment.NewLine, errors.Select (e => e.ToString ()));
				throw new InvalidOperationException ($"Compilation errors detected in project '{project}': {errorMessages}");
			}

			if (!xcSync.TryGetTargetPlatform (Logger, Framework, out string targetPlatform))
				return project;

			IsMauiApp = Scripts.IsMauiAppProject (RootPath);

			TypeService.AddCompilation (targetPlatform, compilation);
		} catch (InvalidOperationException ex) {
			Logger.Error (ex, $"Compilation error in project '{project}': {ex.Message}");
		} catch (NotSupportedException ex) {
			Logger.Error (ex, $"Invalid operation while processing project '{project}': {ex.Message}");
		} catch (Exception ex) {
			Logger.Error (ex, $"Unexpected error occurred while processing project '{project}': {ex.Message}");
		}

		return project;
	}

}

