// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;
using Serilog;
using xcsync.Projects;

namespace xcsync.Workers;

readonly record struct LoadTypesFromObjCMessage (string Id, XcodeWorkspace XcodeWorkspace, ISyncableItem Item);


class ObjCTypesLoader (ILogger Logger): IWorker<LoadTypesFromObjCMessage> {
	public async Task ConsumeAsync (LoadTypesFromObjCMessage message, CancellationToken token = default)
	{
		var visitor = new ObjCImplementationDeclVisitor (Logger);
		visitor.ObjCTypes.CollectionChanged += message.XcodeWorkspace.ProcessObjCTypes;
		await message.XcodeWorkspace.LoadTypesFromObjCFileAsync (((SyncableType) message.Item).FilePath, visitor, token);
		visitor.ObjCTypes.CollectionChanged -= message.XcodeWorkspace.ProcessObjCTypes;
	}
}
