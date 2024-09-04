// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync;

struct ChangeMessage (string id, string path, SyncContextBase context) {
	public string Id { get; set; } = id;
	public string Path { get; set; } = path;
	public SyncContextBase Context { get; set; } = context;
}

class ChangeWorker () : BaseWorker<ChangeMessage> {

	public override Task ConsumeAsync (ChangeMessage message, CancellationToken cancellationToken = default)
	{
		var context = (SyncContext) message.Context; //delaying cast due to accessibility modifiers..
		switch (context.SyncDirection) {
			case SyncDirection.FromXcode:
				return context.SyncFromXcodeAsync (cancellationToken);
			case SyncDirection.ToXcode:
				return context.SyncToXcodeAsync (cancellationToken);
			default:
				throw new InvalidOperationException ("Invalid context type"); //necessary..?
		}
	}
}
