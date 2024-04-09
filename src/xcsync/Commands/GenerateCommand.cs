// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics.CodeAnalysis;
using xcsync.Projects;

namespace xcsync.Commands;

public class GenerateCommand : BaseCommand<GenerateCommand> {

	public static void Execute (string project, string target, bool force, bool open, LogLevel verbosity, string tfm)
	{
		ConfigureLogging (verbosity);

		Logger?.Information ($"Generating files from project '{project}' to target '{target}'");

		if (!TryGetTargetPlatform (tfm, OptionValidations.AppleTfms, out string? targetPlatform))
			return;

		var dotnet = new Dotnet (project);
		var nsProject = new NSProject (dotnet, targetPlatform);
	}

	public static bool TryGetTargetPlatform (string tfm, List<string> supportedTfms, [NotNullWhen(true)] out string? targetPlatform)
	{
		targetPlatform = null;

		if (string.IsNullOrEmpty (tfm) && supportedTfms.Count > 1) {
			Logger?.Fatal ("Multiple target frameworks found in the project, please specify one using --tfm option.");
			return false;
		}

		if (!supportedTfms.Contains (tfm) && supportedTfms.Count > 1) {
			Logger?.Fatal ("Target framework passed in is not supported by current .net project.");
			return false;
		}

		var currentTfm = supportedTfms.Count == 1 ? supportedTfms [0] : tfm;

		foreach (var platform in ApplePlatforms.platforms) {
			if (currentTfm.Contains (platform.Key)) {
				targetPlatform = platform.Key;
				return true;
			}
		}

		Logger?.Fatal ("Target platform not found");
		return false;
	}
}
