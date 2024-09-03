// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using ILogger = Serilog.ILogger;

namespace xcsync.Workers;

readonly record struct FileMessage (string Id, string Path, string Content);

readonly record struct CopyFileMessage (string Id, string sourcePath, string destinationPath);

class FileWorker (ILogger Logger, IFileSystem fileSystem) : BaseWorker<FileMessage> {
	public override async Task ConsumeAsync (FileMessage message, CancellationToken cancellationToken = default)
	{
		try {
			await fileSystem.File.WriteAllTextAsync (message.Path, message.Content, cancellationToken);			
		} catch (Exception ex) {
			Logger?.Fatal ($"Exception in ConsumeAsync: {ex.Message}");
			throw;
		}
	}

	public void Dispose () {}

	public ValueTask DisposeAsync () => ValueTask.CompletedTask;
}
