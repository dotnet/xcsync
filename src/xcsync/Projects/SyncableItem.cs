// Copyright (c) Microsoft Corporation.  All rights reserved.

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
