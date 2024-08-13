// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;
using xcsync.Projects;

namespace xcsync;

interface ISyncableItem {
}

class SyncableContent (string filePath) : ISyncableItem {
	string FilePath => filePath;
}

class SyncableType (TypeMapping typeMap, string filePath) : ISyncableItem {
	string HeaderFileName => $"{typeMap.ObjCType}.h";
	string ModuleFileName => $"{typeMap.ObjCType}.m";

	public string FilePath => filePath;

	public TypeMapping TypeMap => typeMap;

}
