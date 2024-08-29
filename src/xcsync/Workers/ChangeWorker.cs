// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;

namespace xcsync.Workers;

public struct ChangeMessage (string id, string path, object payload) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public ChangeLoad Change { get; set; } = (ChangeLoad) payload;
}

class ChangeWorker () : IWorker<ChangeMessage> {
	public bool UseBackgroundThread => false; //todo: use

	public Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		// todo: impl per load
		return message.Change switch {
			SyncLoad => Task.CompletedTask,
			ErrorLoad => Task.CompletedTask,
			RenameLoad => Task.CompletedTask,
			_ => Task.CompletedTask
		};
	}

	public void Dispose () {}

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;

}

public interface ChangeLoad {
	object ChangeDetected { get; }
}

readonly record struct SyncLoad (object ChangeDetected) : ChangeLoad;
readonly record struct ErrorLoad (object ChangeDetected) : ChangeLoad;
readonly record struct RenameLoad (object ChangeDetected) : ChangeLoad;
