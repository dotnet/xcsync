// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Commands;

public class WatchCommand : BaseCommand<WatchCommand> {

	public static void Execute (string project, string target, bool force, LogLevel verbosity)
	{
		ConfigureLogging (verbosity);

		Logger?.Information (Strings.Watch.HeaderInformation, project, target);
		// Implement logic here
	}
}
