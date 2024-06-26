// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class ContinuousSyncContext (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public List<(ChangeWorker, TaskCompletionSource<bool>)> workers = new();
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
		await hub.CreateAsync<ChangeMessage> (ChangeChannel, configuration);

		using var clrChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New (), Logger);
		clrChanges.StartMonitoring (clrProject, token);
		using var xcodeChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New (), Logger);
		xcodeChanges.StartMonitoring (xcodeProject, token);
		
		clrChanges.OnFileChanged = async path => {
			Logger.Debug ($"CLR Project file {path} changed");
			await SyncChange (path, hub);
		};
		
		xcodeChanges.OnFileChanged = async path => {
			Logger.Debug ($"Xcode Project file {path} changed");
			await SyncChange (path, hub);
		};

		async void ClrFileRenamed (string oldPath, string newPath)
		{
			Logger.Debug ($"CLR Project file {oldPath} renamed to {newPath}");
			await SyncRename (oldPath, hub);
		}

		clrChanges.OnFileRenamed = ClrFileRenamed;
		
		async void XcodeFileRenamed (string oldPath, string newPath)
		{
			Logger.Debug ($"Xcode Project file {oldPath} renamed to {newPath}");
			await SyncRename (oldPath, hub);
		}
		
		xcodeChanges.OnFileRenamed = XcodeFileRenamed;
		
		clrChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in CLR Project file change monitor");
			await SyncError (ProjectPath, ex, hub);
		};
		
		xcodeChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in Xcode Project file change monitor");
			await SyncError (TargetDir, ex, hub);
		};

		do {
			// TODO:  Use a FIFO queue to process the jobs
			// Keep executing sync jobs until the user presses the esc sequence [CTRL-Q]
			Logger.Debug ("Checking for changes in the projects...");
			var result = await Task.WhenAll (workers.Select (x => x.Item2.Task));
			// when all in result are true ==> all changes synced!
			// maybe add logging for status?
		} while (token.IsCancellationRequested == false);
		
		Logger.Information ("User has requested to stop the sync process. Changes will no longer be processed.");
	}

	public async Task SyncChange (string path, IHub hub)
	{
		var workerId = await RegisterChangeWorker (hub);
		// Hub will publish the message to the channel, will be received by worker (who will enact consumeAsync)
		var syncLoad = new SyncLoad (new object ());
		await hub.Publish (ChangeChannel, new ChangeMessage (workerId, path, syncLoad));
	}

	public async Task SyncError (string path, Exception ex, IHub hub)
	{
		var workerId = await RegisterChangeWorker (hub);
		var errorLoad = new ErrorLoad (ex);
		await hub.Publish (ChangeChannel, new ChangeMessage (workerId, path, errorLoad));
	}
	
	public async Task SyncRename (string path, IHub hub)
	{
		var workerId = await RegisterChangeWorker (hub);
		var renameLoad = new RenameLoad (new object ());
		await hub.Publish (ChangeChannel, new ChangeMessage (workerId, path, renameLoad));
	}
	
	public async Task<string> RegisterChangeWorker (IHub hub)
	{
		var correlationId = Guid.NewGuid ();
		var tcs = new TaskCompletionSource<bool> ();
		var worker = new ChangeWorker ($"{correlationId}", tcs);
		workers.Add ((worker, tcs));
		await hub.RegisterAsync (ChangeChannel, worker); 
		// worker now knows to pick up any and all change-related events from the channel in hub
		return worker.Id;
	}
}

