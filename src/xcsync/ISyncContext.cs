// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using xcsync.Projects;

namespace xcsync;

interface ISyncContext {

	ITypeService TypeService { get; }

	Task SyncAsync (CancellationToken token)
	{
		return Task.CompletedTask;
	}
}

