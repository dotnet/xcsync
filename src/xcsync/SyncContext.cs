// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class SyncContext (IFileSystem fileSystem, ITypeService typeService, SyncDirection Direction, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	protected SyncDirection SyncDirection { get; } = Direction;

	public async Task SyncAsync (CancellationToken token = default)
	{
		if (SyncDirection == SyncDirection.ToXcode)
			await SyncToXcodeAsync (token);
		else
			await SyncFromXcodeAsync (token);

		Logger.Debug ("Synchronization complete.");
	}

	async Task SyncToXcodeAsync (CancellationToken token)
	{
		// TODO : Add code to Generate Xcode project from CLR project 
		await Task.Delay (1000, token);
		Logger.Debug ("Generating Xcode project files...");
	}

	async Task SyncFromXcodeAsync (CancellationToken token)
	{
		// TODO : Add code to Generate CLR changes from Xcode project 
		await Task.Delay (1000, token);
		Logger.Debug ("Synchronizing changes from Xcode project...");
	}
}
