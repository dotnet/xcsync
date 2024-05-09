// Copyright (c) Microsoft Corporation. All rights reserved.

namespace xcsync;

interface ISyncableProject {
	string Name { get; }
	string RootPath { get; }
	string[] ProjectFilesFilter { get; }
}
