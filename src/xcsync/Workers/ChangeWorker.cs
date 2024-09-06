// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

struct ChangeMessage (string id, string path, SyncDirection direction) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public SyncDirection Direction { get; set; } = direction;
}

class ChangeWorker (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger) : BaseWorker<ChangeMessage> {
	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default) =>
		new SyncContext (fileSystem, typeService, message.Direction, projectPath, targetDir, framework, logger).SyncAsync (cancellationToken);

	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default)
	{
		Log.Error (exception, "Error processing change message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
