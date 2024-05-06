// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace xcsync.Commands;

public class XcodeCommand<T> : BaseCommand<T> {

	protected bool Force { get; private set; }
	protected bool Open { get; private set; }

	protected Option<bool> force = new (
		["--force", "-f"],
		description: "Force overwrite of an existing Xcode project",
		getDefaultValue: () => false);

	protected Option<bool> open = new (
		["--open", "-o"],
		description: "Open the generated project",
		getDefaultValue: () => false);

	public XcodeCommand (string name, string description) : base (name, description)
	{
		Add (force);
		Add (open);
		AddValidator ((result) => {
			if (!string.IsNullOrEmpty (result.ErrorMessage)) {
				return;
			}

			Force = result.GetValueForOption (force);
			Open = result.GetValueForOption (open);

			var error = ValidateCommand (Force);

			result.ErrorMessage = error;
		});

	}

	/// <summary>
	/// For testing purposes only
	/// </summary>
	/// <param name="name"></param>
	/// <param name="description"></param>
	/// <param name="logger"></param>
	internal XcodeCommand (string name, string description, string projectPath, string tfm, string targetPath, bool force) : base (name, description, projectPath, tfm, targetPath)
	{
		Force = force;
	}


	protected internal string ValidateCommand (bool force)
	{
		LogVerbose ("[Begin] {Command} Validation", nameof (XcodeCommand<T>));

		if (Force) {
			if (FileSystem.DirectoryExists (TargetPath) && FileSystem.EnumerateFileSystemEntries (TargetPath).Any ()) {
				FileSystem.Delete (TargetPath, true);
			}
			if (!FileSystem.DirectoryExists (TargetPath)) {
				FileSystem.CreateDirectory (TargetPath);
			}
		} else {
			if (FileSystem.DirectoryExists (TargetPath) && FileSystem.EnumerateFileSystemEntries (TargetPath).Any ()) {
				LogDebug ($"The target path '{{TargetPath}}' already exists and is not empty. Use [{string.Join (", ", this.force.Aliases)}] to overwrite the existing project.", TargetPath);
				return $"The target path '{TargetPath}' already exists and is not empty. Use [{string.Join (", ", this.force.Aliases)}] to overwrite the existing project.";
			} else if (!FileSystem.DirectoryExists (TargetPath) && string.Compare (TargetPath, DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) != 0) {
				LogDebug ($"The target path '{{TargetPath}}' does not exist. Use [{string.Join (", ", this.force.Aliases)}] to force creation.", TargetPath);
				return $"The target path '{TargetPath}' does not exist. Use [{string.Join (", ", this.force.Aliases)}] to force creation.";
			}
		}
		LogVerbose ("[End] {Command} Validation", nameof (XcodeCommand<T>));
		return string.Empty;
	}
}
