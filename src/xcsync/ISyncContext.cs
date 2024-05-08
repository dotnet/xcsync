// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync;

interface ISyncContext {
	Task SyncAsync (CancellationToken token)
	{
		return Task.CompletedTask;
	}
}

