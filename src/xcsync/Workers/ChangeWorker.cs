// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

record struct ChangeMessage (string Id, string Path, SyncDirection Direction, ProjectFileChangeMonitor ClrMonitor, ProjectFileChangeMonitor XcodeMonitor);

class ChangeWorker (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger, ClrProject clrProject, XcodeWorkspace xcodeProject) : BaseWorker<ChangeMessage> {
	public override async Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		message.ClrMonitor.StopMonitoring ();
		message.XcodeMonitor.StopMonitoring ();
		await new SyncContext (fileSystem, typeService, message.Direction, projectPath, targetDir, framework, logger).SyncAsync (cancellationToken);
		message.ClrMonitor.StartMonitoring (clrProject, cancellationToken);
		message.XcodeMonitor.StartMonitoring (xcodeProject, cancellationToken);
	}

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default) 
	{
		Log.Error (exception, "Error processing change message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
