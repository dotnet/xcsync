// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Marille;

namespace xcsync.Workers;

public abstract class BaseWorker<T> : IWorker<T>, IErrorWorker<T> where T : struct {
	public virtual bool UseBackgroundThread => false;

	public virtual Task ConsumeAsync (T message, CancellationToken token = default) => Task.CompletedTask;

	public virtual Task ConsumeAsync (T message, Exception exception, CancellationToken token = default) => Task.CompletedTask;

	public virtual void Dispose () { }

	public virtual ValueTask DisposeAsync () => ValueTask.CompletedTask;
}
