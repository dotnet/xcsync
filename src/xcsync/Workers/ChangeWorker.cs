// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;

namespace xcsync.Workers;

public struct ChangeMessage (string id, string path, object context) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public object Context { get; set; } = context;
}

class ChangeWorker () : IWorker<ChangeMessage> {
	public bool UseBackgroundThread => false; //todo: use

	public Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
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
