// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;
using xcsync.Projects;

namespace xcsync.Commands;

class SyncCommand : BaseCommand<SyncCommand> {
	public SyncCommand (IFileSystem fileSystem) : base (fileSystem, "sync", "synchronize changes from the Xcode project back to the.NET project")
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		var sync = new SyncContext (fileSystem, new TypeService(), SyncDirection.FromXcode, ProjectPath, TargetPath, Tfm, Logger!);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
