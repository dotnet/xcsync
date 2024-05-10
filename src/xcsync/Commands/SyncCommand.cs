// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;

namespace xcsync.Commands;

class SyncCommand : BaseCommand<SyncCommand> {
	public SyncCommand () : base ("sync", "synchronize changes from the Xcode project back to the.NET project")
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		var sync = new SyncContext (SyncDirection.FromXcode, ProjectPath, TargetPath, Tfm, Logger!);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
