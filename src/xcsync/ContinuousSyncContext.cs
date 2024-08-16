// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

class ContinuousSyncContext (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public const string ChangeChannel = "Changes";
	public async Task SyncAsync (CancellationToken token = default)
	{
		// Generate initial Xcode project
		await new SyncContext (FileSystem, TypeService, SyncDirection.ToXcode, ProjectPath, TargetDir, Framework, Logger).SyncAsync (token);

		var clrProject = new ClrProject (FileSystem, Logger, TypeService, "CLR Project", ProjectPath, Framework);
		var xcodeProject = new XcodeWorkspace (FileSystem, Logger, TypeService, "Xcode Project", TargetDir, Framework);

		configuration.Mode = ChannelDeliveryMode.AtMostOnceSync;
		// Hub creates a topic channel w message type template
		// Only 1 channel corresponding to project changes to model FIFO queue && preserve order
		// Different changes will be processed differently based on unique payload
		await Hub.CreateAsync<ChangeMessage> (ChangeChannel, configuration);
		await RegisterChangeWorker (Hub);

		using var clrChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New (), Logger);
		clrChanges.StartMonitoring (clrProject, token);
		using var xcodeChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New (), Logger);
		xcodeChanges.StartMonitoring (xcodeProject, token);

		clrChanges.OnFileChanged = async path => {
			Logger.Debug ($"CLR Project file {path} changed");
			await SyncChange (path, Hub);
		};

		xcodeChanges.OnFileChanged = async path => {
			Logger.Debug ($"Xcode Project file {path} changed");
			await SyncChange (path, Hub);
		};

		async void ClrFileRenamed (string oldPath, string newPath)
		{
			Logger.Debug ($"CLR Project file {oldPath} renamed to {newPath}");
			await SyncRename (oldPath, Hub);
		}

		clrChanges.OnFileRenamed = ClrFileRenamed;

		async void XcodeFileRenamed (string oldPath, string newPath)
		{
			Logger.Debug ($"Xcode Project file {oldPath} renamed to {newPath}");
			await SyncRename (oldPath, Hub);
		}

		xcodeChanges.OnFileRenamed = XcodeFileRenamed;

		clrChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in CLR Project file change monitor");
			await SyncError (ProjectPath, ex, Hub);
		};

		xcodeChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in Xcode Project file change monitor");
			await SyncError (TargetDir, ex, Hub);
		};

		do {
			// TODO:  Use a FIFO queue to process the jobs
			// Keep executing sync jobs until the user presses the esc sequence [CTRL-Q]
			Logger.Debug ("Checking for changes in the projects...");
		} while (token.IsCancellationRequested == false);

		Logger.Information ("User has requested to stop the sync process. Changes will no longer be processed.");
	}

	public async Task SyncChange (string path, IHub hub)
	{
		// Hub will publish the message to the channel, will be received by worker (who will enact consumeAsync)
		var syncLoad = new SyncLoad (new object ());
		await hub.Publish (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), path, syncLoad));
	}

	public async Task SyncError (string path, Exception ex, IHub hub)
	{
		var errorLoad = new ErrorLoad (ex);
		await hub.Publish (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), path, errorLoad));
	}

	public async Task SyncRename (string path, IHub hub)
	{
		var renameLoad = new RenameLoad (new object ());
		await hub.Publish (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), path, renameLoad));
	}

	public async Task RegisterChangeWorker (IHub hub)
	{
		var worker = new ChangeWorker ();
		await hub.RegisterAsync (ChangeChannel, worker);
		// worker now knows to pick up any and all change-related events from the channel in hub
	}
}

