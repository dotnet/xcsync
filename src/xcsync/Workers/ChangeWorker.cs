// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

public struct ChangeMessage (string id, string path, object payload) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public ChangeLoad Change { get; set; } = (ChangeLoad) payload;
}

class ChangeWorker () : BaseWorker<ChangeMessage> {

	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		// todo: impl per load
		return message.Change switch {
			SyncLoad => Task.CompletedTask,
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
