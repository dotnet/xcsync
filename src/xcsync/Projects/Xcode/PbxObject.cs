// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin.MacDev;

namespace xcsync.Projects.Xcode.Model;

record class PbxObject (PbxProjectFile ProjectFile, PbxGuid Guid, PDictionary Properties) {
	public PbxObject? Parent { get; internal set; }

	public PbxObject (PbxProjectFile projectFile) : this (projectFile, PbxGuid.NewGuid (), []) { }

}
