// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace xcsync;

interface ISyncableProject {
	string Name { get; }
	string RootPath { get; }
	ExtensionFilter ProjectFilesFilter { get; }
}
