// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;

namespace xcsync.Commands;

public class WatchCommand : XcodeCommand<WatchCommand> {
	public WatchCommand () : base ("watch",
			"generates a Xcode project, then continuously synchronizes changes between the Xcode project and the .NET project")
	{
		this.SetHandler (Execute, project, target, tfm, force, open);
	}

	public void Execute (string project, string target, string tfm, bool force, bool open)
	{
		LogInformation ("Continuously syncing files between project '{projectPath}' and target '{targetPath} for '{tfm}' platform ", ProjectPath, TargetPath, Tfm);
		// Implement logic here
	}
}
