// Copyright (c) Microsoft Corporation. All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Build.Tasks;

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

	public XcodeCommand (IFileSystem fileSystem, string name, string description) : base (fileSystem, name, description)
	{
		Add (force);
		Add (open);
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


	protected override (string, string) TryValidateTargetPath (string projectPath, string targetPath)
	{
		string error = string.Empty;

		if (Force) {
			if (fileSystem.Directory.Exists (targetPath) && fileSystem.Directory.EnumerateFileSystemEntries (targetPath).Any ()) {
				fileSystem.Directory.Delete (targetPath, true);
			}

			if (!fileSystem.Directory.Exists (targetPath)) {
				fileSystem.Directory.CreateDirectory (targetPath);
			}
		} else {
			if (fileSystem.Directory.Exists (targetPath) && fileSystem.Directory.EnumerateFileSystemEntries (targetPath).Any ()) {
				LogDebug (Strings.Errors.Validation.TargetNotEmpty (targetPath));
				error = Strings.Errors.Validation.TargetNotEmpty (targetPath);
			}

			if (!fileSystem.Directory.Exists (targetPath) && string.Compare (targetPath, DefaultXcodeOutputFolder, StringComparison.OrdinalIgnoreCase) != 0) {
				LogDebug (Strings.Errors.Validation.TargetDoesNotExist (targetPath));
				error =  Strings.Errors.Validation.TargetDoesNotExist (targetPath);
			}
		}
		return (error, targetPath);
	}
}
