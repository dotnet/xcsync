// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync.Commands;

class WatchCommand : XcodeCommand<WatchCommand> {

	public WatchCommand (IFileSystem fileSystem, ILogger logger) : base (fileSystem, logger, "watch", Strings.Commands.WatchDescription)
	{
		SetAction (ExecuteAsync);
	}

	public async Task ExecuteAsync (ParseResult result, CancellationToken cancellationToken)
	{
		var open = result.GetValue (this.open);
		var force = result.GetValue (this.force);

		using var cts = new CancellationTokenSource ();

		// Occurs when Ctrl+C or Ctrl+Break is pressed.
		Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
		{
			cts.Cancel ();
			LogInformation (Strings.Watch.ReceivedCtrlC);
			e.Cancel = false;
		};

		LogInformation (Strings.Watch.HeaderInformation (ProjectPath, TargetPath, Tfm));

		var sync = new ContinuousSyncContext (fileSystem, new TypeService (Logger!), ProjectPath, TargetPath, Tfm, Logger!, open, force);

		// Start an asynchronous task
		var xcsyncTask = Task.Run (async () => {
			try {
				if (cts.IsCancellationRequested)
					return;
				await sync.SyncAsync (cts.Token).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				LogInformation (Strings.Watch.StopSynchronization);
			}
		}, cts.Token);

		LogInformation (Strings.Watch.ExitWatchInstructions (!Console.IsInputRedirected));
		var consoleReader = new ConsoleReader (cts.Token);
		consoleReader.Start ();

		// Wait for user input (e.g., ESC key) to cancel the task
		while (cts.IsCancellationRequested == false) {
			if (consoleReader.Next == (int) ConsoleKey.Escape) {
				LogInformation (Strings.Watch.ReceivedEsc);
				cts.Cancel ();
			}
		}

		await xcsyncTask;
	}

	class ConsoleReader {
		readonly BlockingCollection<int> buffer = new (1);
		readonly Task readTask;
		readonly CancellationToken token;

		public ConsoleReader (CancellationToken token)
		{
			this.token = token;
			readTask = new Task (() => {
				if (Console.IsInputRedirected) {
					int i;
					do {
						i = Console.Read ();
						buffer.Add (i);
					} while (i != -1 && token.IsCancellationRequested == false);
				} else {
					while (token.IsCancellationRequested == false) {
						var consoleKeyInfo = Console.ReadKey (true);
						if (consoleKeyInfo.KeyChar == 0) continue;  // ignore dead keys
						buffer.Add (consoleKeyInfo.KeyChar);
					}
				}
			}, token);
		}

		public void Start ()
		{
			readTask.Start ();
		}

		public int? Next {
			get {
				if (!token.IsCancellationRequested)
					return buffer.TryTake (out int result, 0) ? result : default (int?);
				else
					return default (int?);
			}
		}
	}
}
