// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Serilog;

namespace xcsync.Commands;

class BaseCommand<T> : Command {
	protected const string DefaultXcodeOutputFolder = "obj/xcode";
	protected static ILogger? Logger { get; private set; }

	/// <summary>
	/// Path to the source .NET .csproj
	/// </summary>
	protected string ProjectPath { get; private set; } = string.Empty;

	/// <summary>
	/// Path to the target Xcode project
	/// </summary>
	protected string TargetPath { get; private set; } = string.Empty;

	/// <summary>
	/// Target Framework Moniker (ex: net8.0-ios)
	/// </summary>
	protected string Tfm { get; private set; } = string.Empty;

	/// <summary>
	/// Target Apple Platform (ex: ios)
	/// </summary>
	protected string TargetPlatform => TryGetTargetPlatform (Tfm, out string targetPlatform) ? targetPlatform : string.Empty;
	protected readonly IFileSystem fileSystem;

	protected Option<string> project = new (
		["--project", "-p"],
		description: Strings.Options.ProjectDescription,
		getDefaultValue: () => ".");

	protected Option<string> tfm = new (
		["--target-framework-moniker", "-tfm"],
		description: Strings.Options.TfmDescription,
		getDefaultValue: () => string.Empty);

	protected Option<string> target = new (
		["--target", "-t"],
		description: Strings.Options.TargetDescription,
		getDefaultValue: () => $".{Path.DirectorySeparatorChar}{DefaultXcodeOutputFolder}");

	public BaseCommand (IFileSystem fileSystem, ILogger logger, string name, string description) : base (name, description)
	{
		Logger = logger ?? xcSync.Logger?
			.ForContext ("SourceContext", typeof (T).Name.Replace ("Command", string.Empty).ToLowerInvariant ());

		this.fileSystem = fileSystem ?? throw new ArgumentNullException (nameof (fileSystem));

		AddOptions ();

		AddValidators ();
	}

	protected virtual void AddOptions ()
	{
		Add (project);
		Add (tfm);
		Add (target);
	}

	protected virtual void AddValidators ()
	{
		AddValidator ((result) => {

			if (!RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				result.ErrorMessage = Strings.Errors.Validation.InvalidOS;
				return;
			}

			xcSync.XcodePath = Scripts.SelectXcode ();
			Logger?.Information (Strings.Base.XcodePath (xcSync.XcodePath));

			var validation = ValidateCommand (result);

			ProjectPath = validation.ProjectPath;
			Tfm = validation.Tfm;
			TargetPath = validation.TargetPath;
			result.ErrorMessage = validation.Error;
		});
	}

	internal record struct ValidationResult (string ProjectPath, string Tfm, string TargetPath, string Error);

	internal ValidationResult ValidateCommand (CommandResult result)
	{
		string error;
		var projectPath = result.GetValueForOption (project) ?? string.Empty;
		var targetPath = result.GetValueForOption (target) ?? string.Empty;
		var moniker = result.GetValueForOption (tfm) ?? string.Empty;

		(error, string newProjectPath) = TryValidateProjectPath (projectPath);
		if (!string.IsNullOrEmpty (error)) { return new ValidationResult (projectPath, moniker, targetPath, error); }

		(error, string newTfm) = TryValidateTfm (projectPath, moniker);
		if (!string.IsNullOrEmpty (error)) { return new ValidationResult (newProjectPath, moniker, targetPath, error); }

		(error, string newTargetPath) = TryValidateTargetPath (projectPath, targetPath);
		if (!string.IsNullOrEmpty (error)) { return new ValidationResult (newProjectPath, newTfm, targetPath, error); }

		return new ValidationResult (newProjectPath, newTfm, newTargetPath, error);
	}

	protected virtual (string, string) TryValidateProjectPath (string projectPath)
	{
		string error = string.Empty;
		var updatedPath = projectPath;

		if (!fileSystem.Path.GetExtension (projectPath).Equals (".csproj", StringComparison.OrdinalIgnoreCase) && !fileSystem.File.Exists (projectPath) && !fileSystem.Directory.Exists (projectPath)) {
			LogDebug (Strings.Errors.Validation.PathDoesNotContainCsproj (projectPath));
			error = Strings.Errors.Validation.PathDoesNotContainCsproj (projectPath);
			return (error, projectPath);
		}

		if (!fileSystem.Path.GetExtension (projectPath).Equals (".csproj", StringComparison.OrdinalIgnoreCase) && fileSystem.Directory.Exists (projectPath)) {
			// We have been given a directory, let's see if we can find a .csproj file
			LogDebug (Strings.Base.SearchForProjectFiles (projectPath));

			var csprojFiles = fileSystem.Directory.EnumerateFiles (projectPath, "*.csproj", SearchOption.TopDirectoryOnly)
												  .Select ((path) => fileSystem.Path.GetRelativePath (fileSystem.Path.Combine (".", fileSystem.Path.GetDirectoryName (projectPath) ?? string.Empty), path))
												  .ToArray ();

			if (csprojFiles.Length == 0) {
				LogDebug (Strings.Errors.CsprojNotFound (projectPath));
				error = Strings.Errors.CsprojNotFound (projectPath);
				return (error, projectPath);
			}
			if (csprojFiles.Length > 1) {
				LogDebug (Strings.Errors.MultipleProjectFilesFound (projectPath, string.Join (", ", csprojFiles)));
				error = Strings.Errors.MultipleProjectFilesFound (projectPath, string.Join (", ", csprojFiles));
				return (error, projectPath);
			}

			LogDebug (Strings.Base.FoundProjectFile (csprojFiles [0], projectPath));
			updatedPath = csprojFiles [0];
		}

		if (!fileSystem.File.Exists (updatedPath)) {
			LogDebug (Strings.Errors.Validation.PathDoesNotExist (updatedPath));
			error = Strings.Errors.Validation.PathDoesNotExist (updatedPath);
			return (error, projectPath);
		}

		projectPath = updatedPath;
		return (error, projectPath);
	}

	protected virtual (string, string) TryValidateTfm (string projectPath, string tfm)
	{
		string error = string.Empty;

		if (!TryGetTfmFromProject (projectPath, out var supportedTfmsInProject)) {
			LogDebug (Strings.Errors.Validation.MissingTfmInPath (projectPath));
			error = Strings.Errors.Validation.MissingTfmInPath (projectPath);
			return (error, tfm);
		}

		LogDebug (Strings.Base.ProjectTfms (string.Join (", ", supportedTfmsInProject)));

		if (string.IsNullOrEmpty (tfm) && supportedTfmsInProject.Count > 1) {
			LogDebug (Strings.Errors.MultipleTfmsFound);
			error = Strings.Errors.MultipleTfmsFound;
			return (error, tfm);
		}

		if (string.IsNullOrEmpty (tfm) && supportedTfmsInProject.Count == 1) {
			LogDebug (Strings.Base.UseDefaultTfm);
			tfm = supportedTfmsInProject [0];
		}

		if (!supportedTfmsInProject.Contains (tfm)) {
			LogDebug (Strings.Errors.TfmNotSupported);
			error = Strings.Errors.TfmNotSupported;
			return (error, tfm);
		}

		var currentTfm = supportedTfmsInProject.Count == 1 ? supportedTfmsInProject [0] : tfm;

		if (!IsValidTfm (currentTfm, xcSync.ApplePlatforms.Keys.ToArray ())) {
			LogDebug (Strings.Errors.Validation.InvalidTfm (currentTfm));
			error = Strings.Errors.Validation.InvalidTfm (currentTfm);
			return (error, currentTfm);
		}

		tfm = currentTfm;
		return (error, tfm);
	}

	protected virtual (string, string) TryValidateTargetPath (string projectPath, string targetPath)
	{
		string error = string.Empty;

		if (targetPath.EndsWith (DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty (targetPath)) {
			LogVerbose (Strings.Base.EstablishDefaultTarget (fileSystem.Path.GetDirectoryName (projectPath)!));
			targetPath = fileSystem.Path.Combine (fileSystem.Path.GetDirectoryName (projectPath) ?? ".", DefaultXcodeOutputFolder);

			if (!fileSystem.Directory.Exists (targetPath)) {
				LogDebug (Strings.Base.CreateDefaultTarget (targetPath));
				fileSystem.Directory.CreateDirectory (targetPath);
			}
		}

		if (!fileSystem.Directory.Exists (targetPath)) {
			LogDebug (Strings.Errors.Validation.TargetDoesNotExist (targetPath));
			error = Strings.Errors.Validation.TargetDoesNotExist (targetPath);
			return (error, targetPath);
		}

		return (error, targetPath);
	}

	internal bool TryGetTfmFromProject (string csproj, [NotNullWhen (true)] out List<string> tfms)
	{

		tfms = [];
		try {
			tfms = Scripts.GetTargetFrameworksFromProject (fileSystem, csproj);

			return tfms.Count > 0;
		} catch (Exception ex) {
			Logger?.Error (ex, "Failed to get TFM's from project {project}", csproj);
			return false;
		}
	}

	static bool IsValidTfm (string tfm, string []? supportedPlatforms = null)
	{
		string regexMatch = supportedPlatforms is null || supportedPlatforms.Length == 0 ?
			$@"^net\d+\.\d+(-[a-zA-Z]+)?(?:\d+\.\d+)?$" :
			$@"^net\d+\.\d+-({string.Join ("|", supportedPlatforms)})(?:\d+\.\d+)?$";

		var isValid = Regex.IsMatch (tfm, regexMatch);
		return isValid;
	}

	static bool TryGetTargetPlatform (string tfm, [NotNullWhen (true)] out string targetPlatform)
	{
		targetPlatform = string.Empty;

		foreach (var platform in xcSync.ApplePlatforms) {
			if (tfm.Contains (platform.Key)) {
				targetPlatform = platform.Key;
				return true;
			}
		}

		Logger?.Fatal (Strings.Errors.TargetPlatformNotFound);
		return false;
	}

	static protected void LogVerbose (string messageTemplate, params object? []? properyValues) => Logger?.Verbose (messageTemplate, properyValues);
	static protected void LogDebug (string messageTemplate, params object? []? properyValues) => Logger?.Debug (messageTemplate, properyValues);
	static protected void LogInformation (string messageTemplate, params object? []? properyValues) => Logger?.Information (messageTemplate, properyValues);
	static protected void LogError (string messageTemplate, params object? []? properyValues) => Logger?.Error (messageTemplate, properyValues);
	static protected void LogFatal (string messageTemplate, params object? []? properyValues) => Logger?.Fatal (messageTemplate, properyValues);

}
