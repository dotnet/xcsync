// Copyright (c) Microsoft Corporation.  All rights reserved.

using xcsync.Projects;

namespace xcsync;

interface ISyncContext {

	ITypeService TypeService { get; }
	
	Task SyncAsync (CancellationToken token)
	{
		return Task.CompletedTask;
	}
}

