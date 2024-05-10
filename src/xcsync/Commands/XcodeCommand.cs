// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;

namespace xcsync.Commands;

class XcodeCommand<T> : BaseCommand<T> {

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

	public XcodeCommand (IFileSystem fileSystem, string name, string description) : base (fileSystem, name, description)
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
	internal XcodeCommand (IFileSystem fileSystem, string name, string description, string projectPath, string tfm, string targetPath, bool force) : base (fileSystem, name, description, projectPath, tfm, targetPath)
	{
		Force = force;
	}


	protected internal string ValidateCommand (bool force)
	{
		LogVerbose ("[Begin] {Command} Validation", nameof (XcodeCommand<T>));

		if (Force) {
			if (fileSystem.Directory.Exists (TargetPath) && fileSystem.Directory.EnumerateFileSystemEntries (TargetPath).Any ()) {
				fileSystem.Directory.Delete (TargetPath, true);
			}
			if (!fileSystem.Directory.Exists (TargetPath)) {
				fileSystem.Directory.CreateDirectory (TargetPath);
			}
		} else {
			if (fileSystem.Directory.Exists (TargetPath) && fileSystem.Directory.EnumerateFileSystemEntries (TargetPath).Any ()) {
				LogDebug ($"The target path '{{TargetPath}}' already exists and is not empty. Use [{string.Join (", ", this.force.Aliases)}] to overwrite the existing project.", TargetPath);
				return $"The target path '{TargetPath}' already exists and is not empty. Use [{string.Join (", ", this.force.Aliases)}] to overwrite the existing project.";
			} else if (!fileSystem.Directory.Exists (TargetPath) && string.Compare (TargetPath, DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) != 0) {
				LogDebug ($"The target path '{{TargetPath}}' does not exist. Use [{string.Join (", ", this.force.Aliases)}] to force creation.", TargetPath);
				return $"The target path '{TargetPath}' does not exist. Use [{string.Join (", ", this.force.Aliases)}] to force creation.";
			}
		}
		LogVerbose ("[End] {Command} Validation", nameof (XcodeCommand<T>));
		return string.Empty;
	}
}
