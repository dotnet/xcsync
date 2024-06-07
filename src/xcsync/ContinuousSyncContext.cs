// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class ContinuousSyncContext (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {
	public async Task SyncAsync (CancellationToken token = default)
	{
		// Generate initial Xcode project
		await new SyncContext (FileSystem, TypeService, SyncDirection.ToXcode, ProjectPath, TargetDir, Framework, Logger).SyncAsync (token);


		var clrProject = new ClrProject (FileSystem, Logger, "CLR Project", ProjectPath, Framework);
		var xcodeProject = new XcodeWorkspace (FileSystem, Logger, "Xcode Project", TargetDir, Framework);

		using var clrChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New(), Logger);
		clrChanges.StartMonitoring (clrProject, token);
		using var xcodeChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New(), Logger);
		xcodeChanges.StartMonitoring (xcodeProject, token);

		// TODO: Create new jobs for type / file changes and add them to the Queue
		do {
			// TODO:  Use a FIFO queue to process the jobs
			// Keep executing sync jobs until the user presses the esc sequence [CTRL-Q]
			await Task.Delay (1000, token); // Run next Job
			Logger.Debug ("Checking for changes in the projects...");
		} while (token.IsCancellationRequested == false);
	}
}

