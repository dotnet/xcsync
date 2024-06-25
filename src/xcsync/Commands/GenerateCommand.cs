// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;
using xcsync.Projects;

namespace xcsync.Commands;

class GenerateCommand : XcodeCommand<GenerateCommand> {

	public GenerateCommand (IFileSystem fileSystem) : base (fileSystem, "generate",
			"generate a Xcode project at the path specified by --target from the project identified by --project")
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		Logger?.Information (Strings.Generate.HeaderInformation, ProjectPath, TargetPath);

		var sync = new SyncContext (fileSystem, new TypeService (), SyncDirection.ToXcode, ProjectPath, TargetPath, Tfm, Logger!, Open);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
