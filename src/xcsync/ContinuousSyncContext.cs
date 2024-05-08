// Copyright (c) Microsoft Corporation.  All rights reserved.

using Serilog;
using xcsync.Projects;

namespace xcsync;

class ContinuousSyncContext (string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (projectPath, targetDir, framework, logger) {

	public async Task SyncAsync (CancellationToken token = default)
	{
		// Generate initial Xcode project
		await new SyncContext (SyncDirection.ToXcode, ProjectPath, TargetDir, Framework, Logger).SyncAsync (token);

		// TODO: Initialize projects
		var ClrProject = new Dotnet (ProjectPath, Framework);
		// var XcodeProject = new XcodeWorkspace (TargetDir);

		// TODO: Monitor changes in the projects e.g.
		// using var dotnetChanges = new ProjectFileChangeMonitor (ClrProject);
		// dotnetChanges.StartMonitoring (token);
		// using var xcodeChanges = new ProjectFileChangeMonitor (XcodeProject);
		// xcodeChanges.StartMonitoring (token);

		// TODO: Create new jobs for type / file changes and add them to the Queue
		do {
			// TODO:  Use a FIFO queue to process the jobs
			// Keep executing sync jobs until the user presses the esc sequence [CTRL-Q]
			await Task.Delay (1000, token); // Run next Job
			Logger.Information ("Checking for changes in the projects...");
		} while (token.IsCancellationRequested == false);
	}
}

