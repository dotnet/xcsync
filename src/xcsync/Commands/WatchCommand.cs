// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;

namespace xcsync.Commands;

public class WatchCommand : XcodeCommand<WatchCommand> {
	bool keepRunning = true;

	public WatchCommand () : base ("watch",
			"generates a Xcode project, then continuously synchronizes changes between the Xcode project and the .NET project")
	{
		this.SetHandler (Execute, project, target, tfm, force, open);
	}

	public async Task Execute (string project, string target, string tfm, bool force, bool open)
	{
		Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;
			keepRunning = false;
		};

		LogInformation ("Continuously syncing files between project '{projectPath}' and target '{targetPath} for '{tfm}' platform, press [ESC] to end.", ProjectPath, TargetPath, Tfm);
		using var cts = new CancellationTokenSource ();

		var sync = new ContinuousSyncContext (ProjectPath, TargetPath, Tfm, Logger!);

		// Start an asynchronous task
		var task = Task.Run (async () => {
			try {
				await sync.SyncAsync (cts.Token).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				LogInformation ("Stopping synchronization and completing remaining jobs.");
			}
		});

		// Wait for user input (e.g., ESC key) to cancel the task
		do {
			keepRunning = !Console.KeyAvailable && Console.ReadKey (true).Key != ConsoleKey.Escape;
			await Task.Delay (500);
		} while (keepRunning);

		// Cancel the task
		cts.Cancel ();

		// Wait for the task to complete (optional)
		await task;
	}
}
