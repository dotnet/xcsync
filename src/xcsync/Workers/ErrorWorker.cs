
using Marille;
using Serilog;

namespace xcsync.Workers;

class ChangeErrorWorker : IErrorWorker <ChangeMessage> {
	public bool UseBackgroundThread => false;

	public Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default) {
		Log.Error (exception, "Error processing change message {Id}", message.Id);
		return Task.CompletedTask;
	}

	public void Dispose () { }

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;
}

class FileErrorWorker : IErrorWorker<FileMessage> {
	public bool UseBackgroundThread => false;

	public Task ConsumeAsync (FileMessage message, Exception exception, CancellationToken token = default) {
		Log.Error (exception, "Error processing file message {Id}", message.Id);
		return Task.CompletedTask;
	}

	public void Dispose () {}

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;
}

class ObjCTypeLoaderErrorWorker : IErrorWorker<LoadTypesFromObjCMessage> {
	public bool UseBackgroundThread => false;

	public Task ConsumeAsync (LoadTypesFromObjCMessage message, Exception exception, CancellationToken token = default) {
		Log.Error (exception, "Error processing ObjC type loader message {Id}", message.Id);
		return Task.CompletedTask;
	}

	public void Dispose () {}

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;
}
