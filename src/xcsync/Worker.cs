using Marille;

namespace xcsync;

public interface IBasicMessage { // prolly not necessary...
	string Id { get; set; }
	string Path { get; set; }
}

public struct BasicMessage : IBasicMessage {
	public string Id { get; set; }
	public string Path { get; set; }
}

public struct SyncMessage : IBasicMessage {
	public string Id { get; set; }
	public string Path { get; set; }
	public string ChangeType { get; set; } // eh
	
	public SyncMessage (string id, string path, string changeType)
	{
		Id = id;
		Path = path;
		ChangeType = changeType;
	}
}

public struct ErrorMessage : IBasicMessage {
	public string Id { get; set; }
	public string Path { get; set; }
	public Exception Ex { get; set; }
	
	public ErrorMessage (string id, string path, Exception ex)
	{
		Id = id;
		Path = path;
		Ex = ex;
	}
}

public class BasicWorker : IWorker<BasicMessage> {
	public string Id { get; set; } = string.Empty;
	public TaskCompletionSource<bool> Completion { get; set; } = new();
	
	public BasicWorker (string id, TaskCompletionSource<bool> tcs)
	{
		Id = id;
		Completion = tcs;
	}

	// impl default change logic here?
	public Task ConsumeAsync (BasicMessage message, CancellationToken cancellationToken = default)
		=> Task.FromResult (Completion.TrySetResult(true));
}
public class SyncWorker : BasicWorker {
	public SyncWorker (string id, TaskCompletionSource<bool> tcs) : base(id, tcs) {}

	//todo: sync changes impl
	public Task ConsumeAsync (SyncMessage message, CancellationToken cancellationToken = default)
		=> Task.FromResult (Completion.TrySetResult(true));
}

public class ErrorWorker : BasicWorker {
	public ErrorWorker (string id, TaskCompletionSource<bool> tcs) : base(id, tcs) {}
	
	//todo: impl error handling logic here
	public Task ConsumeAsync (ErrorMessage message, CancellationToken cancellationToken = default)
		=> Task.FromResult (Completion.TrySetResult(true));
}
