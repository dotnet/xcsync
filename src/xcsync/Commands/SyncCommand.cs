// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Commands;

public class SyncCommand : BaseCommand<SyncCommand> {
	public static void Execute (string project, string target, bool force, LogLevel verbosity)
	{
		ConfigureLogging (verbosity);

		Logger?.Information ("Syncing files from project {Project} to target {Target}", project, target);
	}
}
