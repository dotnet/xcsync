// Copyright (c) Microsoft Corporation.  All rights reserved.

using Serilog;
using Serilog.Events;

namespace xcsync.Commands;

public class BaseCommand<T> {
	public static ILogger? Logger { get; private set; }

	public static void ConfigureLogging (LogLevel verbosity)
	{
		Logger = new LoggerConfiguration ()
					.MinimumLevel.Is (verbosity switch {
						LogLevel.Fatal => LogEventLevel.Fatal,
						LogLevel.Error => LogEventLevel.Error,
						LogLevel.Information => LogEventLevel.Information,
						LogLevel.Debug => LogEventLevel.Debug,
						LogLevel.Verbose => LogEventLevel.Verbose,
						_ => LogEventLevel.Information,
					})
					.Enrich.WithThreadName ()
					.Enrich.WithThreadId ()
					.Enrich.FromLogContext ()
					.WriteTo.Console (outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {SourceContext} ({syncId}) ({ThreadId}) {Message}{NewLine}{Exception}",
										theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Sixteen)
					.CreateLogger ()
					.ForContext ("SourceContext", typeof (T).Name.Replace ("Command", string.Empty).ToLowerInvariant ());
	}

}
