// Copyright (c) Microsoft Corporation.  All rights reserved.

using Marille;

namespace xcsync.Workers;

public struct ChangeMessage (string id, string path, object payload) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public ChangeLoad Change { get; set; } = (ChangeLoad) payload;
}

class ChangeWorker (TaskCompletionSource<bool> tcs) : IWorker<ChangeMessage> {
	public TaskCompletionSource<bool> Completion { get; set; } = tcs;

	public Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		// todo: impl per load
		return message.Change switch {
			SyncLoad => Task.FromResult (Completion.TrySetResult(true)),
			ErrorLoad => Task.FromResult (Completion.TrySetResult(true)),
			RenameLoad => Task.FromResult (Completion.TrySetResult(true)),
			_ => Task.FromResult (Completion.TrySetResult (true))
		};
	}
}

public interface ChangeLoad {
	object ChangeDetected { get; }
}

readonly record struct SyncLoad (object ChangeDetected) : ChangeLoad;
readonly record struct ErrorLoad (object ChangeDetected) : ChangeLoad;
readonly record struct RenameLoad (object ChangeDetected) : ChangeLoad;
