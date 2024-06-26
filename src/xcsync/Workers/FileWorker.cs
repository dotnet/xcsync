// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Marille;
using ILogger = Serilog.ILogger;

namespace xcsync.Workers;

readonly record struct FileMessage (string Id, string Path, string Content);

class FileWorker (ILogger Logger, TaskCompletionSource<bool> tcs, IFileSystem fileSystem) : IWorker<FileMessage> {
	TaskCompletionSource<bool> Completion { get; set; } = tcs;

	public async Task ConsumeAsync (FileMessage message, CancellationToken cancellationToken = default)
	{
		try {
			await fileSystem.File.WriteAllTextAsync (message.Path, message.Content, cancellationToken);
			Completion.TrySetResult (true);
		} catch (Exception ex) {
			Logger?.Fatal ($"Exception in ConsumeAsync: {ex.Message}");
			Completion.TrySetResult (false);
			throw;
		}
	}
}
