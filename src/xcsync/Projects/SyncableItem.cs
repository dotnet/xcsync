// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using xcsync.Projects;

namespace xcsync;

interface ISyncableItem {
}

class SyncableContent (string sourcePath, string destinationPath) : ISyncableItem {
	public string SourcePath => sourcePath;
	public string DestinationPath => destinationPath;
}

class SyncableType (TypeMapping typeMap, string filePath) : ISyncableItem {
	string HeaderFileName => $"{typeMap.ObjCType}.h";
	string ModuleFileName => $"{typeMap.ObjCType}.m";

	public string FilePath => filePath;

	public TypeMapping TypeMap => typeMap;

}
