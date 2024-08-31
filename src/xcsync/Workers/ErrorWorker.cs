
using Marille;

namespace xcsync.Workers;

class ChangeErrorWorker : BaseErrorWorker<ChangeMessage> {
	public override Task ConsumeAsync (ChangeMessage message, Exception exception, CancellationToken token = default) 
		=> Task.CompletedTask;
}

class FileErrorWorker : BaseErrorWorker<FileMessage> {
	public override Task ConsumeAsync (FileMessage message, Exception exception, CancellationToken token = default) 
		=> Task.CompletedTask;
}

class ObjCTypeLoaderErrorWorker : BaseErrorWorker<LoadTypesFromObjCMessage> {
	public override Task ConsumeAsync (LoadTypesFromObjCMessage message, Exception exception, CancellationToken token = default) 
		=> Task.CompletedTask;
}
