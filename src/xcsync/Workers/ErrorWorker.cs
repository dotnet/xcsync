
using Marille;

namespace xcsync.Workers;

class ChangeErrorWorker : IErrorWorker <ChangeMessage> {
	public bool UseBackgroundThread => throw new NotImplementedException ();

	public Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default)
	{
		throw new NotImplementedException ();
	}

	public void Dispose ()
	{
		throw new NotImplementedException ();
	}

	public ValueTask DisposeAsync ()
	{
		throw new NotImplementedException ();
	}
}

class FileErrorWorker : IErrorWorker<FileMessage> {
	public bool UseBackgroundThread => throw new NotImplementedException ();

	public Task ConsumeAsync (FileMessage message, Exception exception, CancellationToken token = default)
	{
		throw new NotImplementedException ();
	}

	public void Dispose ()
	{
		throw new NotImplementedException ();
	}

	public ValueTask DisposeAsync ()
	{
		throw new NotImplementedException ();
	}
}

class ObjCTypeLoaderErrorWorker : IErrorWorker<LoadTypesFromObjCMessage> {
	public bool UseBackgroundThread => throw new NotImplementedException ();

	public Task ConsumeAsync (LoadTypesFromObjCMessage message, Exception exception, CancellationToken token = default)
	{
		throw new NotImplementedException ();
	}

	public void Dispose ()
	{
		throw new NotImplementedException ();
	}

	public ValueTask DisposeAsync ()
	{
		throw new NotImplementedException ();
	}
}
