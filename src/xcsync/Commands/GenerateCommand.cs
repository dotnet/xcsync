// Copyright (c) Microsoft Corporation.  All rights reserved.

using xcsync.Projects;

namespace xcsync.Commands;

public class GenerateCommand : BaseCommand<GenerateCommand> {

	public static void Execute (string project, string target, bool force, bool open, LogLevel verbosity)
	{
		ConfigureLogging (verbosity);

		Logger?.Information ($"Generating files from project '{project}' to target '{target}'");

		var dotnet = new Dotnet (project);
		var nsProject = new NSProject (dotnet);
	}
}
