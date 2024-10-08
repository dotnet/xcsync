// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;
using xcsync.Projects;

namespace xcsync.Workers;

readonly record struct LoadTypesFromObjCMessage (string Id, TaskCompletionSource CompletionSource, XcodeWorkspace XcodeWorkspace, ISyncableItem Item);

class ObjCTypesLoader (ILogger Logger) : BaseWorker<LoadTypesFromObjCMessage> {
	public override async Task ConsumeAsync (LoadTypesFromObjCMessage message, CancellationToken token = default)
	{
		var visitor = new ObjCImplementationDeclVisitor (Logger);
		visitor.ObjCTypes.CollectionChanged += message.XcodeWorkspace.ProcessObjCTypes;
		await message.XcodeWorkspace.LoadTypesFromObjCFileAsync (((SyncableType) message.Item).FilePath, visitor, token);
		message.CompletionSource.SetResult ();
		visitor.ObjCTypes.CollectionChanged -= message.XcodeWorkspace.ProcessObjCTypes;
	}

	public override Task ConsumeAsync (LoadTypesFromObjCMessage message, Exception exception, CancellationToken token = default)
	{
		Log.Error (exception, "Error processing ObjC type loader message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
