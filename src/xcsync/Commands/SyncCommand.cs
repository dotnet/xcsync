// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync.Commands;

class SyncCommand : BaseCommand<SyncCommand> {
	protected Option<bool> explicitTypes = new (
		["--explicit-types", "-e"],
		description: Strings.Options.ExplicitTypesDescription,
		getDefaultValue: () => false);

	public SyncCommand (IFileSystem fileSystem, ILogger logger) : base (fileSystem, logger, "sync", Strings.Commands.SyncDescription)
	{
		this.SetHandler (Execute, explicitTypes);
	}

	protected override void AddOptions ()
	{
		base.AddOptions ();
		Add (explicitTypes);
	}

	public async Task Execute (bool explicitTypes)
	{
		Logger?.Information (Strings.Sync.HeaderInformation, TargetPath, ProjectPath);

		var sync = new SyncContext (fileSystem, new TypeService (Logger!), SyncDirection.FromXcode, ProjectPath, TargetPath, Tfm, Logger!, explicitTypes: explicitTypes);
		await sync.SyncAsync ().ConfigureAwait (false);
	}
}
