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

	protected override (string, string) TryValidateTargetPath (string projectPath, string tfm, string targetPath)
	{
		string error = string.Empty;
		var intermediateOutputPath = Scripts.GetIntermediateOutputPath (projectPath, tfm);

		targetPath = targetPath.Replace ("$(IntermediateOutputPath)", intermediateOutputPath);

		if (!fileSystem.Path.IsPathRooted (targetPath)) {
			targetPath = fileSystem.Path.Combine (fileSystem.Path.GetDirectoryName (projectPath)!, targetPath.Replace ("$(IntermediateOutputPath)", intermediateOutputPath));
		}

		if (!Force) {
			if (fileSystem.Directory.Exists (targetPath) && fileSystem.Directory.EnumerateFileSystemEntries (targetPath).Any ()) {
				LogDebug (Strings.Errors.Validation.TargetNotEmpty (targetPath));
				error = Strings.Errors.Validation.TargetNotEmpty (targetPath);
			}

			if (!fileSystem.Directory.Exists (targetPath) && !targetPath.EndsWith (fileSystem.Path.Combine (intermediateOutputPath, DefaultXcodeOutputFolder), StringComparison.OrdinalIgnoreCase)) {
				LogDebug (Strings.Errors.Validation.TargetDoesNotExist (targetPath));
				error = Strings.Errors.Validation.TargetDoesNotExist (targetPath);
			}
		}
		return (error, targetPath);
	}
}
