// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Commands;

public class WatchCommand {
	public static void Execute (string project, string target, bool force)
	{
		Console.WriteLine ($"Watching files from project '{project}' to target '{target}'");
		// Implement logic here
	}
}
