// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync.Commands;

class GenerateCommand : XcodeCommand<GenerateCommand> {

	public GenerateCommand (IFileSystem fileSystem, ILogger logger) : base (fileSystem, logger, "generate", Strings.Commands.SyncDescription)
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		Logger?.Information (Strings.Generate.HeaderInformation, ProjectPath, TargetPath);

		var sync = new SyncContext (fileSystem, new TypeService (Logger!), SyncDirection.ToXcode, ProjectPath, TargetPath, Tfm, Logger!, Open, Force);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
