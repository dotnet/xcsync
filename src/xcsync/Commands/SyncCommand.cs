// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Commands;

public class SyncCommand : BaseCommand<SyncCommand> {
	public static void Execute (string project, string target, bool force, LogLevel verbosity)
	{
		ConfigureLogging (verbosity);

		Logger?.Information (Strings.Sync.HeaderInformation, project, target);
	}
}
