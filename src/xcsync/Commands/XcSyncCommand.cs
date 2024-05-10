// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using Serilog.Events;

namespace xcsync.Commands;

class XcSyncCommand : RootCommand {

	public static ILogger? Logger { get; private set; }

	static XcSyncCommand ()
	{
		Logger = xcSync.Logger!
					.ForContext ("SourceContext", typeof (XcSyncCommand).Name.Replace ("Command", string.Empty).ToLowerInvariant ());
	}

	public XcSyncCommand (IFileSystem fileSystem) : base ("xcsync")
	{
		AddCommand (new GenerateCommand (fileSystem));
		AddCommand (new SyncCommand (fileSystem));
		AddCommand (new WatchCommand (fileSystem));

		AddGlobalOption (SharedOptions.Verbose);
		SharedOptions.Verbose.AddValidator (result => {
			var value = result.GetValueForOption (SharedOptions.Verbose);
			if (!result.IsImplicit && result.Tokens.Count == 0) {
				value = Verbosity.Normal;
			}
			xcSync.LogLevelSwitch.MinimumLevel = value switch {
				Verbosity.Quiet => LogEventLevel.Error,
				Verbosity.Minimal => LogEventLevel.Error,
				Verbosity.Normal => LogEventLevel.Information,
				Verbosity.Detailed => LogEventLevel.Verbose,
				Verbosity.Diagnostic => LogEventLevel.Verbose,
				_ => LogEventLevel.Information,
			};

		});
	}
}
