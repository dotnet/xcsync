// Copyright (c) Microsoft Corporation.  All rights reserved.
using xcsync.Projects;

namespace xcsync.Commands;

public class GenerateCommand {
	public static async Task Execute (string project, string target, bool force, bool open)
	{
		Console.WriteLine ($"Generating files from project '{project}' to target '{target}'");

		var dotnet = new Dotnet (project);
		var openProject = await dotnet.OpenProject ().ConfigureAwait (false);
		await foreach (var type in dotnet.GetTypes (openProject).ConfigureAwait (false)) {
			// process each type
		}
	}
}
