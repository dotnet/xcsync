// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Commands;

public class SyncCommand {
	public static void Execute (string project, string target, bool force)
	{
		Console.WriteLine ($"Syncing files from project '{project}' to target '{target}'");
		// Implement logic here
	}
}
