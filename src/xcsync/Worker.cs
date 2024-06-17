using Marille;

namespace xcsync;

public struct ChangeMessage {
	public string Id { get; set; }
	public string Path { get; set; }
	public object Payload { get; set; }
	
	public ChangeMessage (string id, string path, object payload)
	{
		Id = id;
		Path = path;
		Payload = payload;
	}
}

public class ChangeWorker : IWorker<ChangeMessage> {
	public string Id { get; set; } = string.Empty;
	public TaskCompletionSource<bool> Completion { get; set; } = new();
	
	public ChangeWorker (string id, TaskCompletionSource<bool> tcs)
	{
		Id = id;
		Completion = tcs;
	}

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

public struct SyncLoad {
	//todo: fine tune payload
	public object ChangeDetected { get; set; }
	public SyncLoad (object changeDetected)
	{
		ChangeDetected = changeDetected;
	}
}
public struct ErrorLoad {
	//todo: fine tune payload
	public Exception Ex { get; set; }
	public ErrorLoad (Exception ex)
	{
		Ex = ex;
	}
}

public struct RenameLoad {
	//todo: fine tune payload
	public object ChangeDetected { get; set; }
	public RenameLoad (object changeDetected)
	{
		ChangeDetected = changeDetected;
	}
}
