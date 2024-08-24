// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;

namespace xcsync.Workers;

struct ChangeMessage (string id, string path, ProjectFileChangeMonitor monitor, SyncContext context, object payload) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public ProjectFileChangeMonitor Monitor { get; set; } = monitor;
	public SyncContext Context { get; set; } = context;

	// is this payload necessary here..? esp w more specific channels avail in lib?
	public ChangeLoad Change { get; set; } = (ChangeLoad) payload;
}

class ChangeWorker () : IWorker<ChangeMessage> {

	public Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		// todo: impl per load
		return message.Change switch {
			SyncLoad => message.Context.SyncAsync (cancellationToken),
			ErrorLoad => Task.CompletedTask,
			RenameLoad => Task.CompletedTask,
			_ => Task.CompletedTask
		};
	}
}

public interface ChangeLoad {
	object ChangeDetected { get; }
}

readonly record struct SyncLoad (object ChangeDetected) : ChangeLoad;
readonly record struct ErrorLoad (object ChangeDetected) : ChangeLoad;
readonly record struct RenameLoad (object ChangeDetected) : ChangeLoad;
