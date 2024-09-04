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
	public SyncContext Context = new (fileSystem, typeService, SyncDirection.ToXcode, projectPath, targetDir, framework, logger);

	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		switch (message.Direction) {
			case SyncDirection.FromXcode:
				return Context.SyncFromXcodeAsync (cancellationToken);
			case SyncDirection.ToXcode:
				return Context.SyncToXcodeAsync (cancellationToken);
			default:
				throw new InvalidOperationException ("Invalid direction type detected");
		}
	}
}
