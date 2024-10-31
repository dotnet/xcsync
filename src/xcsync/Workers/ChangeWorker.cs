// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

record struct ChangeMessage (string Id, string Path, SyncDirection Direction, ProjectFileChangeMonitor ClrMonitor, ProjectFileChangeMonitor XcodeMonitor);

class ChangeWorker (IFileSystem FileSystem, string ProjectPath, string TargetDir, string Framework, ILogger Logger, ClrProject ClrProject, XcodeWorkspace XcodeProject) : BaseWorker<ChangeMessage> {
	public override async Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		Logger.Debug (Strings.Watch.PausingMonitoring);
		message.ClrMonitor.StopMonitoring ();
		message.XcodeMonitor.StopMonitoring ();
		Logger.Debug (Strings.Watch.Syncing);
		await new SyncContext (FileSystem, new TypeService (Logger), message.Direction, ProjectPath, TargetDir, Framework, Logger, Open: false, force: message.Direction == SyncDirection.ToXcode).SyncAsync (cancellationToken);
		Logger.Debug (Strings.Watch.ResumingMonitoring);
		message.ClrMonitor.StartMonitoring (ClrProject, cancellationToken);
		message.XcodeMonitor.StartMonitoring (XcodeProject, cancellationToken);
	}

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default)
	{
		Logger.Fatal (Strings.Watch.WorkerException (message.Id, exception.Message));
		//TODO: https://github.com/dotnet/xcsync/issues/82
		return Task.CompletedTask;
	}
}
