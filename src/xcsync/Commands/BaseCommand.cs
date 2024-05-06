// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Serilog;

namespace xcsync.Commands;

public class BaseCommand<T> : Command {
	protected static string DefaultXcodeOutputFolder => Path.Combine ("obj", "xcode");
	protected static ILogger? Logger { get; private set; }

	protected string ProjectPath { get; private set; } = string.Empty;
	protected string TargetPath { get; private set; } = string.Empty;
	protected string Tfm { get; private set; } = string.Empty;

	protected Option<string> project = new (
		["--project", "-p"],
		description: "Path to the source .NET project",
		getDefaultValue: () => ".");

	protected Option<string> tfm = new (
		["--target-framework-moniker", "-tfm"],
		description: "Specify the target framework moniker",
		getDefaultValue: () => string.Empty);

	protected Option<string> target = new (
		["--target", "-t"],
		description: "Path to the folder for the Xcode project",
		getDefaultValue: () => Path.Combine (".", DefaultXcodeOutputFolder));

	static BaseCommand ()
	{
		Logger = xcSync.Logger?
					.ForContext ("SourceContext", typeof (T).Name.Replace ("Command", string.Empty).ToLowerInvariant ());
	}

	public BaseCommand (string name, string description) : base (name, description)
	{
		Add (project);
		Add (tfm);
		Add (target);
		AddValidator ((result) => {
			var projectPath = result.GetValueForOption (project) ?? string.Empty;
			var targetPath = result.GetValueForOption (target) ?? string.Empty;
			var moniker = result.GetValueForOption (tfm) ?? string.Empty;

			var error = ValidateCommand (ref projectPath, ref moniker, ref targetPath);

			ProjectPath = projectPath;
			Tfm = moniker;
			TargetPath = targetPath;
			result.ErrorMessage = error;
		});
	}

	/// <summary>
	/// For testing purposes only
	/// </summary>
	/// <param name="name"></param>
	/// <param name="description"></param>
	/// <param name="logger"></param>
	internal BaseCommand (string name, string description, string projectPath, string tfm, string targetPath) : base (name, description)
	{
		ProjectPath = projectPath;
		Tfm = tfm;
		TargetPath = targetPath;
	}

	protected internal virtual string ValidateCommand (ref string projectPath, ref string moniker, ref string targetPath)
	{
		LogVerbose ("[Begin] {Command} Validation", nameof (BaseCommand<T>));

		bool valid = true;
		string error = string.Empty;

		(valid, error) = TryValidateProjectPath (ref projectPath);
		if (!valid) { return error; }

		(valid, error) = TryValidateTfm (projectPath, ref moniker);
		if (!valid) { return error; }

		(valid, error) = TryValidateTargetPath (ProjectPath, ref targetPath);
		if (!valid) { return error; }

		LogVerbose ("[End] {Command} Validation", nameof (XcodeCommand<T>));
		return error;
	}

	(bool, string) TryValidateProjectPath (ref string projectPath)
	{
		string error = string.Empty;
		var updatedPath = projectPath;

		if (!FileSystem.GetExtension (projectPath).Equals (".csproj", StringComparison.OrdinalIgnoreCase) && !FileSystem.FileExists (projectPath) && !FileSystem.DirectoryExists (projectPath)) {
			LogDebug ("'{projectPath}' is not a valid path to a .NET project or a directory.", projectPath);
			error = $"'{projectPath}' is not a valid path to a .NET project or a directory.";
			return (false, error);
		}

		if (!FileSystem.GetExtension (projectPath).Equals (".csproj", StringComparison.OrdinalIgnoreCase) && FileSystem.DirectoryExists (projectPath)) {
			// We have been given a directory, let's see if we can find a .csproj file
			LogDebug ("'{projectPath}' is not a .csproj file, searching for .csproj files in directory", projectPath);
			var csprojFiles = FileSystem.EnumerateFiles (projectPath, "*.csproj", SearchOption.TopDirectoryOnly).ToArray ();
			if (csprojFiles.Length == 0) {
				LogDebug ("No .csproj files found in '{projectPath}'", projectPath);
				error = $"No .csproj files found in '{projectPath}'";
				return (false, error);
			}
			if (csprojFiles.Length > 1) {
				LogDebug ("Multiple .csproj files found in '{projectPath}', please specify the project file to use: {csprojFiles}", projectPath, string.Join (", ", csprojFiles));
				error = $"Multiple .csproj files found in '{projectPath}', please specify the project file to use: {string.Join (", ", csprojFiles)}";
				return (false, error);
			}
			LogDebug ("Found .csproj file '{csprojFile}' in '{projectPath}'", csprojFiles [0], projectPath);
			updatedPath = csprojFiles [0];
		}
		if (!FileSystem.FileExists (updatedPath)) {
			LogDebug ("File not found: '{projectPath}'", updatedPath);
			error = $"File not found: '{updatedPath}'";
			return (false, error);
		}
		projectPath = updatedPath;
		return (true, error);
	}

	(bool, string) TryValidateTfm (string projectPath, ref string tfm)
	{
		string error = string.Empty;

		if (!TryGetTfmFromProject (projectPath, out var supportedTfmsInProject)) {
			LogDebug ("No target framework monikers found in '{projectPath}'", projectPath);
			error = $"No target framework monikers found in '{projectPath}'";
			return (false, error);
		}

		LogDebug ("Project target frameworks: {supportedTfmsInProject}", string.Join (", ", supportedTfmsInProject));

		if (string.IsNullOrEmpty (tfm) && supportedTfmsInProject.Count > 1) {
			LogDebug ($"Multiple target frameworks found in the project, please specify one using [{string.Join (", ", this.tfm.Aliases)}] option.");
			error = $"Multiple target frameworks found in the project, please specify one using [{string.Join (", ", this.tfm.Aliases)}] option.";
			return (false, error);
		}

		if (string.IsNullOrEmpty (tfm) && supportedTfmsInProject.Count == 1) {
			LogDebug ("No target framework moniker specified, using the first one found in the project.");
			tfm = supportedTfmsInProject [0];
		}

		if (!supportedTfmsInProject.Contains (tfm)) {
			LogDebug ("Target framework is not supported by current .net project.");
			error = "Target framework is not supported by current .net project.";
			return (false, error);
		}

		var currentTfm = supportedTfmsInProject.Count == 1 ? supportedTfmsInProject [0] : tfm;

		if (!IsValidTfm (currentTfm, xcSync.ApplePlatforms.Keys.ToArray ())) {
			LogDebug ("Target framework moniker '{currentTfm}' does not target Apple platforms.'", currentTfm, projectPath);
			error = $"Target framework moniker '{currentTfm}' does not target Apple platforms.";
			return (false, error);
		}

		tfm = currentTfm;
		return (true, error);
	}

	(bool, string) TryValidateTargetPath (string projectPath, ref string targetPath)
	{
		string error = string.Empty;
		LogVerbose ("[Begin] {Method}]", nameof (TryValidateTargetPath));
		if (targetPath.EndsWith (DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty (targetPath)) {
			LogVerbose ("Target path is the default location or empty, prefixing with '{projectPath}'", FileSystem.GetDirectoryName (projectPath));
			targetPath = FileSystem.Combine (FileSystem.GetDirectoryName (projectPath) ?? ".", DefaultXcodeOutputFolder);
			if (!Directory.Exists (targetPath)) {
				LogDebug ("Target path '{targetPath}' does not exist, but is the default location, creating.", targetPath);
				FileSystem.CreateDirectory (targetPath);
			}
		}
		if (!FileSystem.DirectoryExists (targetPath)) {
			LogDebug ("Target path '{targetPath}' does not exist, will create directory if [--force, -f] is set.", targetPath);
			error = $"Target path '{targetPath}' does not exist, will create directory if [--force, -f] is set.";
			return (false, error);
		}

		LogVerbose ("[End] {Method}]", nameof (TryValidateTargetPath));
		return (true, error);
	}

	static bool TryGetTfmFromProject (string csproj, [NotNullWhen (true)] out List<string>? tfms)
	{
		// TODO: This should an MSBuild target to get the valid TFMs, this currently will not suppoprt 
		// cases where the TFM is set via an <Import/> or Directory.Build.props
		// BUG: This will not return all the TFMs for a standard .NET MAUI project
		tfms = null;
		try {
			var csprojDocument = XDocument.Load (csproj);

			tfms = (csprojDocument.Descendants ("TargetFramework").FirstOrDefault () ??
					 csprojDocument.Descendants ("TargetFrameworks").FirstOrDefault ())?
				   .Value.Split (';').ToList ();

			return tfms is not null && tfms.Count > 0;
		} catch {
			// in case there are issues when loading the file
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

	static protected void LogVerbose (string messageTemplate, params object? []? properyValues) => Logger?.Verbose (messageTemplate, properyValues);
	static protected void LogDebug (string messageTemplate, params object? []? properyValues) => Logger?.Debug (messageTemplate, properyValues);
	static protected void LogInformation (string messageTemplate, params object? []? properyValues) => Logger?.Information (messageTemplate, properyValues);
	static protected void LogError (string messageTemplate, params object? []? properyValues) => Logger?.Error (messageTemplate, properyValues);
	static protected void LogFatal (string messageTemplate, params object? []? properyValues) => Logger?.Fatal (messageTemplate, properyValues);

}
