// Copyright (c) Microsoft Corporation.  All rights reserved.

using Serilog;
using xcsync.Projects;

namespace xcsync;

interface ISyncableItem {
}

class SyncableContent (string filePath) : ISyncableItem {
	string FilePath => filePath;
}

class SyncableType (TypeMapping typeMap) : ISyncableItem {
	string HeaderFileName => $"{typeMap.ObjCType}.h";
	string ModuleFileName => $"{typeMap.ObjCType}.m";

	TypeMapping TypeMap => typeMap;

}

class SyncableFiles (XcodeWorkspace xcodeWorkspace, IEnumerable<string> paths, ILogger logger, CancellationToken cancellationToken = default) : ISyncableItem {
	internal Task ExecuteAsync ()
	{
		var visitor = new ObjCImplementationDeclVisitor (logger);
		visitor.ObjCTypes.CollectionChanged += xcodeWorkspace.ProcessObjCTypes;
		xcodeWorkspace.LoadObjCTypesFromFiles (paths, visitor, cancellationToken);
		visitor.ObjCTypes.CollectionChanged -= xcodeWorkspace.ProcessObjCTypes;
		return Task.CompletedTask;
	}
}
