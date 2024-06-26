using Marille;

namespace xcsync;

public struct ChangeMessage (string id, string path, object payload) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public object Payload { get; set; } = payload;
}

public class ChangeWorker (string id, TaskCompletionSource<bool> tcs) : IWorker<ChangeMessage> {
	public string Id { get; set; } = id;
	public TaskCompletionSource<bool> Completion { get; set; } = tcs;

	public Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		// todo: impl per load
		return message.Payload switch {
			SyncLoad => Task.FromResult (Completion.TrySetResult(true)),
			ErrorLoad => Task.FromResult (Completion.TrySetResult(true)),
			RenameLoad => Task.FromResult (Completion.TrySetResult(true)),
			_ => Task.FromResult (Completion.TrySetResult (true))
		};
	}
}

readonly record struct SyncLoad (object ChangeDetected);

readonly record struct ErrorLoad (Exception Ex);

readonly record struct RenameLoad (object ChangeDetected);
