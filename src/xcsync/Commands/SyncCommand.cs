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
		SetAction (ExecuteAsync);
	}

	public async Task ExecuteAsync (ParseResult result, CancellationToken cancellationToken)
	{
		Logger?.Information (Strings.Sync.HeaderInformation, TargetPath, ProjectPath);

		var sync = new SyncContext (fileSystem, new TypeService (Logger!), SyncDirection.FromXcode, ProjectPath, TargetPath, Tfm, Logger!);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
