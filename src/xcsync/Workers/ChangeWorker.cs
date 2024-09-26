// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Commands;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

record struct ChangeMessage (string Id, string Path, SyncDirection Direction, ProjectFileChangeMonitor ClrMonitor, ProjectFileChangeMonitor XcodeMonitor);

class ChangeWorker (IFileSystem FileSystem, ITypeService TypeService, string ProjectPath, string TargetDir, string Framework, ILogger Logger, ClrProject ClrProject, XcodeWorkspace XcodeProject) : BaseWorker<ChangeMessage> {
	public override async Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		message.ClrMonitor.StopMonitoring ();
		message.XcodeMonitor.StopMonitoring ();
		if (message.Direction == SyncDirection.ToXcode)
			XcodeCommand<GenerateCommand>.RecreateDirectory (FileSystem, TargetDir);
		await new SyncContext (FileSystem, TypeService, message.Direction, ProjectPath, TargetDir, Framework, Logger).SyncAsync (cancellationToken);
		message.ClrMonitor.StartMonitoring (ClrProject, cancellationToken);
		message.XcodeMonitor.StartMonitoring (XcodeProject, cancellationToken);
	}

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default)
	{
		Log.Error (exception, "Error processing change message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
