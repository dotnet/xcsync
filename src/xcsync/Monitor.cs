// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

class Monitor (Action<TimeSpan> task) : IDisposable
{
	readonly Action<TimeSpan> task = task;
	readonly Stopwatch sw = Stopwatch.StartNew ();

	public void Dispose()
	{
		sw.Stop();
		task(sw.Elapsed);
	}
}