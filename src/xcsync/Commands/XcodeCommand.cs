// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;

namespace xcsync.Commands;

class XcodeCommand<T> : BaseCommand<T> {

	protected bool Force { get; private set; }
	protected bool Open { get; private set; }

	protected Option<bool> force = new (
		["--force", "-f"],
		description: Strings.Options.ForceDescription,
		getDefaultValue: () => false);

	protected Option<bool> open = new (
		["--open", "-o"],
		description: Strings.Options.OpenDescription,
		getDefaultValue: () => false);

	public XcodeCommand (IFileSystem fileSystem, ILogger logger, string name, string description) : base (fileSystem, logger, name, description)
	{ }

	protected override void AddOptions ()
	{
		base.AddOptions ();
		Add (force);
		Add (open);
	}

	protected override void AddValidators ()
	{
		AddValidator ((result) => {
			Force = result.GetValueForOption (force);
			Open = result.GetValueForOption (open);
		});
		base.AddValidators ();
	}

	protected override (string, string) TryValidateTargetPath (string projectPath, string targetPath)
	{
		string error = string.Empty;

		if (targetPath.EndsWith (DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty (targetPath)) {
			Logger.Verbose (Strings.Base.EstablishDefaultTarget (fileSystem.Path.GetDirectoryName (projectPath)!));
			targetPath = fileSystem.Path.Combine (fileSystem.Path.GetDirectoryName (projectPath) ?? ".", DefaultXcodeOutputFolder);
		}

		if (Force) {
			RecreateDirectory (fileSystem, projectPath, targetPath, Logger);
		} else {
			if (fileSystem.Directory.Exists (targetPath) && fileSystem.Directory.EnumerateFileSystemEntries (targetPath).Any ()) {
				Logger.Debug (Strings.Errors.Validation.TargetNotEmpty (targetPath));
				error = Strings.Errors.Validation.TargetNotEmpty (targetPath);
			}

			if (!fileSystem.Directory.Exists (targetPath) && string.Compare (targetPath, DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) != 0) {
				Logger.Debug (Strings.Errors.Validation.TargetDoesNotExist (targetPath));
				error = Strings.Errors.Validation.TargetDoesNotExist (targetPath);
			}
		}
		return (error, targetPath);
	}

	public static void RecreateDirectory (IFileSystem fileSystem, string projectPath, string targetPath, ILogger logger)
	{
		if (fileSystem.Directory.Exists (targetPath) && fileSystem.Directory.EnumerateFileSystemEntries (targetPath).Any ()) {
			// utilize apple script to close existing xcode project if open in xcode
			// quite performant, prevents UI freakout, enables smooth smooth development
			string xcodeProjPath = fileSystem.Path.GetFullPath (fileSystem.Path.Combine (targetPath, fileSystem.Path.GetFileNameWithoutExtension(projectPath) + ".xcodeproj"));
			logger.Debug ("Closing Xcode project at {xcodeProjPath}", xcodeProjPath);
			Scripts.RunAppleScript (Scripts.CloseXcodeProject (xcodeProjPath));

			fileSystem.Directory.Delete (targetPath, true);
		}

		if (!fileSystem.Directory.Exists (targetPath)) {
			fileSystem.Directory.CreateDirectory (targetPath);
		}
	}
}
