// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;

namespace xcsync.Commands;

public class SyncCommand : BaseCommand<SyncCommand> {
	public SyncCommand () : base ("sync", "synchronize changes from the Xcode project back to the.NET project")
	{
		this.SetHandler (Execute);
	}

	public void Execute ()
	{
		LogInformation ("Syncing files from project {Project} to target {Target} for {TFM} platform ", ProjectPath, TargetPath, Tfm);
	}
}
