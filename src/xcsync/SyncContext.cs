// Copyright (c) Microsoft Corporation.  All rights reserved.

using Serilog;

namespace xcsync;

class SyncContext (SyncDirection Direction, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (projectPath, targetDir, framework, logger) {

	protected SyncDirection SyncDirection { get; } = Direction;

	public async Task SyncAsync (CancellationToken token = default)
	{
		if (SyncDirection == SyncDirection.ToXcode)
			await SyncToXcodeAsync (token);
		else
			await SyncFromXcodeAsync (token);

		Logger.Information ("Synchronization complete.");
	}

	async Task SyncToXcodeAsync (CancellationToken token)
	{
		// TODO : Add code to Generate Xcode project from CLR project 
		await Task.Delay (1000, token);
		Logger.Information ("Generating Xcode project files...");
	}

	async Task SyncFromXcodeAsync (CancellationToken token)
	{
		// TODO : Add code to Generate CLR changes from Xcode project 
		await Task.Delay (1000, token);
		Logger.Information ("Synchronizing changes from Xcode project...");
	}
}
