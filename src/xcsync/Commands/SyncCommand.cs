// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync.Commands;

class SyncCommand : BaseCommand<SyncCommand> {
	public SyncCommand (IFileSystem fileSystem, ILogger logger) : base (fileSystem, logger, "sync", Strings.Commands.SyncDescription)
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		Logger?.Information (Strings.Sync.HeaderInformation, TargetPath, ProjectPath);

		var sync = new SyncContext (fileSystem, new TypeService (Logger!), SyncDirection.FromXcode, ProjectPath, TargetPath, Tfm, Logger!);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
