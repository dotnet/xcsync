// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
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

	public XcSyncCommand () : base ("xcsync")
	{
		AddCommand (new GenerateCommand ());
		AddCommand (new SyncCommand ());
		AddCommand (new WatchCommand ());

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
