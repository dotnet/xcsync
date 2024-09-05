// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

struct ChangeMessage (string id, string path, SyncDirection direction, ProjectFileChangeMonitor clrMonitor, ProjectFileChangeMonitor xcodeMonitor) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public SyncDirection Direction { get; set; } = direction;
	public ProjectFileChangeMonitor ClrMonitor { get; set; } = clrMonitor;
	public ProjectFileChangeMonitor XcodeMonitor { get; set; } = xcodeMonitor;
}

class ChangeWorker (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger) : BaseWorker<ChangeMessage> {
	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default) 
	{
		message.ClrMonitor.StopMonitoring ();
		message.XcodeMonitor.StopMonitoring ();
		await new SyncContext (fileSystem, typeService, message.Direction, projectPath, targetDir, framework, logger).SyncAsync (cancellationToken);
		// message.ClrMonitor.StartMonitoring ();
		// message.XcodeMonitor.StartMonitoring ();
		return Task.CompletedTask;
	}

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default) 
	{
		Log.Error (exception, "Error processing change message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
