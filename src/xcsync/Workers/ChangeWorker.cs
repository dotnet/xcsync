// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

public struct ChangeMessage (string id, string path, object context) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public object Context { get; set; } = context;
}

class ChangeWorker () : BaseWorker<ChangeMessage> {

	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		var context = (SyncContext) message.Context; //delaying cast due to accessibility modifiers..
		switch (context.SyncDirection) {
			case SyncDirection.FromXcode:
				return context.SyncFromXcodeAsync (cancellationToken);
			case SyncDirection.ToXcode:
				return context.SyncToXcodeAsync (cancellationToken);
			default:
				throw new InvalidOperationException ("Invalid context type"); //necessary..?
		}
	}

	// protected virtual void Dispose (bool disposing) { }

	// public void Dispose () 
	// {
	// 	Dispose (true);
	// 	GC.SuppressFinalize (this);
	// }

	public void Dispose () { }

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;

}
