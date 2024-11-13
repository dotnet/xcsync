// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class ContinuousSyncContext (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public const string ChangeChannel = "Changes";
	ClrProject ClrProject { get; } = new (fileSystem, logger, typeService, "CLR", projectPath, framework);
	XcodeWorkspace XcodeProject { get; } = new (fileSystem, logger, typeService, "Xcode", targetDir, framework);

	public async Task SyncAsync (CancellationToken token = default)
	{
		await ConfigureMarilleHub ();

		// Generate initial Xcode project
		await new SyncContext (FileSystem, new TypeService (Logger), SyncDirection.ToXcode, ProjectPath, TargetDir, Framework.ToString (), Logger).SyncAsync (token);

		using var clrChanges = new ProjectFileChangeMonitor (FileSystem, FileSystem.FileSystemWatcher.New (), Logger);
		clrChanges.StartMonitoring (ClrProject, token);

		using var xcodeChanges = new ProjectFileChangeMonitor (FileSystem, FileSystem.FileSystemWatcher.New (), Logger);
		xcodeChanges.StartMonitoring (XcodeProject, token);

		clrChanges.OnFileChanged = async path => {
			await Hub.PublishAsync (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), path, SyncDirection.ToXcode, clrChanges, xcodeChanges));
		};

		xcodeChanges.OnFileChanged = async path => {
			await Hub.PublishAsync (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), path, SyncDirection.FromXcode, clrChanges, xcodeChanges));
		};

		async void ClrFileRenamed (string oldPath, string newPath)
		{
			await Hub.PublishAsync (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), newPath, SyncDirection.ToXcode, clrChanges, xcodeChanges));
		}

		clrChanges.OnFileRenamed = ClrFileRenamed;

		async void XcodeFileRenamed (string oldPath, string newPath)
		{
			await Hub.PublishAsync (ChangeChannel, new ChangeMessage (Guid.NewGuid ().ToString (), newPath, SyncDirection.FromXcode, clrChanges, xcodeChanges));
		}

		xcodeChanges.OnFileRenamed = XcodeFileRenamed;

		clrChanges.OnError = ex => {
			// TODO: Send Error to Marrille Error Channel
		};

		xcodeChanges.OnError = ex => {
			// TODO: Send Error to Marrille Error Channel
		};

		do {
			// Keep executing sync jobs until the process is canceled
			await Task.Delay (10); // Just so we don't hog the thread.
		} while (token.IsCancellationRequested == false);

		Logger.Information (Strings.Watch.StopWatchProcess);
	}

	protected async override Task ConfigureMarilleHub ()
	{
		await base.ConfigureMarilleHub ();
		// Hub creates a topic channel w message type template
		// Only 1 channel corresponding to project changes to model FIFO queue && preserve order
		// Different changes will be processed differently based on unique payload
		var worker = new ChangeWorker (FileSystem, ProjectPath, TargetDir, Framework.ToString (), Logger, ClrProject, XcodeProject);
		await Hub.CreateAsync (ChangeChannel, configuration, worker);
		await Hub.RegisterAsync (ChangeChannel, worker);
		// worker now knows to pick up any and all change-related events from the channel in hub
	}
}

