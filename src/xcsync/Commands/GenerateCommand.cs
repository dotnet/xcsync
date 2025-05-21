// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync.Commands;

class GenerateCommand : XcodeCommand<GenerateCommand> {

	public GenerateCommand (IFileSystem fileSystem, ILogger logger) : base (fileSystem, logger, "generate", Strings.Commands.GenerateDescription)
	{
		SetAction (ExecuteAsync);
	}

	public async Task ExecuteAsync (ParseResult result, CancellationToken cancellationToken)
	{
		Logger?.Information (Strings.Generate.HeaderInformation, ProjectPath, TargetPath);

		var sync = new SyncContext (fileSystem, new TypeService (Logger!), SyncDirection.ToXcode, ProjectPath, TargetPath, Tfm, Logger!, Open, Force);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
