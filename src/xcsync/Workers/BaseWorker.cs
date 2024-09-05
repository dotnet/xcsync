// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;

namespace xcsync.Workers;

public abstract class BaseWorker<T> : IWorker<T>, IErrorWorker<T> where T : struct {
	public virtual bool UseBackgroundThread => false;

	public abstract Task ConsumeAsync (T message, CancellationToken token = default);

	public abstract Task ConsumeAsync (T message, Exception exception, CancellationToken token = default);

	public virtual void Dispose () { }

	public virtual ValueTask DisposeAsync () => ValueTask.CompletedTask;
}
