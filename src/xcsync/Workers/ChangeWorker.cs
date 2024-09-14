// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

record struct ChangeMessage (string Id, string Path, SyncDirection Direction, ProjectFileChangeMonitor ClrMonitor, ProjectFileChangeMonitor XcodeMonitor);

class ChangeWorker (IFileSystem FileSystem, ITypeService TypeService, string ProjectPath, string TargetDir, string Framework, ILogger Logger, ClrProject ClrProject, XcodeWorkspace XcodeProject) : BaseWorker<ChangeMessage> {
	public override async Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		Logger.Debug ("Pausing monitoring");
		message.ClrMonitor.StopMonitoring ();
		message.XcodeMonitor.StopMonitoring ();
		Logger.Debug ("Syncing change context");
		await new SyncContext (FileSystem, TypeService, message.Direction, ProjectPath, TargetDir, Framework, Logger).SyncAsync (cancellationToken);
		Logger.Debug ("Context synced. Resuming monitoring");
		message.ClrMonitor.StartMonitoring (ClrProject, cancellationToken);
		message.XcodeMonitor.StartMonitoring (XcodeProject, cancellationToken);
	}

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default)
	{
		Logger.Error (exception, "Error processing change message {Id}: {Message}", message.Id, exception.Message);
		//TODO: if exception is encountered, need to do a better job of alerting and loudly stopping
		// not a quiet failure and seeminlgy try to continue
		return Task.CompletedTask;
	}
}
