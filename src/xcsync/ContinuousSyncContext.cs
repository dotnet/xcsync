// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class ContinuousSyncContext (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public List<(BasicWorker, TaskCompletionSource<bool>)> workers = new();
	public async Task SyncAsync (CancellationToken token = default)
	{
		// Generate initial Xcode project
		await new SyncContext (FileSystem, TypeService, SyncDirection.ToXcode, ProjectPath, TargetDir, Framework, Logger).SyncAsync (token);

		var clrProject = new ClrProject (FileSystem, Logger, TypeService, "CLR Project", ProjectPath, Framework);
		var xcodeProject = new XcodeWorkspace (FileSystem, Logger, TypeService, "Xcode Project", TargetDir, Framework);

		configuration.Mode = ChannelDeliveryMode.AtMostOnceSync;
		// Hub creates a topic channel w message type template
		// Should only be 1 channel corresponding to project changes to model the FIFO queue
		// and ensure all changes to both projects are accounted for in order
		
		// todo: issue..2 sep channels --> !guarantee order of changes
		// notice error, immediately stop sync process? what if mid-change?
		// await hub.CreateAsync<SyncMessage> ("Sync", configuration);
		// await hub.CreateAsync<ErrorMessage> ("Error", configuration);
		
		using var clrChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New(), Logger);
		clrChanges.StartMonitoring (clrProject, token);
		using var xcodeChanges = new ProjectFileChangeMonitor (FileSystem.FileSystemWatcher.New(), Logger);
		xcodeChanges.StartMonitoring (xcodeProject, token);

		// TODO: Create new jobs for type / file changes and add them to the Queue
		
		clrChanges.OnFileChanged = async path => {
			Logger.Debug ($"CLR Project file {path} changed");
			await SyncChannel ("Sync", hub);
		};
		
		xcodeChanges.OnFileChanged = async path => {
			Logger.Debug ($"Xcode Project file {path} changed");
			await SyncChannel ("Sync", hub);
		};
		
		// clrChanges.OnFileRenamed = async path => {
		// 	Logger.Debug ($"CLR Project file {path} renamed");
		// 	await ChannelStuff ("ProjectChanges");
		// };
		
		// xcodeChanges.OnFileRenamed = async path => {
		// 	Logger.Debug ($"Xcode Project file {path} changed");
		// 	await ChannelStuff ("FileRenamed");
		// };
		
		clrChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in CLR Project file change monitor");
			await ErrorChannel (ProjectPath, ex, hub);
		};
		
		xcodeChanges.OnError = async ex => {
			Logger.Error (ex, $"Error:{ex.Message} in Xcode Project file change monitor");
			await ErrorChannel (TargetDir, ex, hub);
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

	public async Task SyncChannel (string topic, IHub hub)
	{
		// shouldn't really be an issue if called multiple times...idempotent via source code iirc
		await hub.CreateAsync<SyncMessage> ("Sync", configuration);
		var correlationId = Guid.NewGuid (); // have some way to uniquely ID the change
		var tcs = new TaskCompletionSource<bool> ();
		var worker = new SyncWorker ($"{correlationId}", tcs);
		workers.Add ((worker, tcs));
		
		// Hub will register the worker to the topic channel
		// In this case ALL sync workers will be registered to the sync channel
		await hub.RegisterAsync (topic, worker);

		// Hub will publish the message to the channel, will be received by worker (who will consumeAsync)
		SyncMessage message = new SyncMessage ($"{correlationId}", "tmp path", "tmp change");
		await hub.Publish (topic, message);
	}

	public async Task ErrorChannel (string path, Exception ex, IHub hub)
	{
		// shouldn't really be an issue if called multiple times...idempotent via source code iirc
		await hub.CreateAsync<ErrorMessage> ("Error", configuration);
		var correlationId = Guid.NewGuid (); // have some way to uniquely ID the change
		var tcs = new TaskCompletionSource<bool> ();
		var worker = new ErrorWorker ($"{correlationId}", tcs); 
		// key reason why it is so impt that we are using ERROR worker here as opposed to sync worker..
		// the consume async method implementation will be different
		workers.Add ((worker, tcs));
		
		await hub.RegisterAsync ("Error", worker); // this is really the key part where the worker knows to pick up any and all events from the error channel/topic in hub
		await hub.Publish ("Error", new ErrorMessage ($"{correlationId}", path, ex));
	}
}

