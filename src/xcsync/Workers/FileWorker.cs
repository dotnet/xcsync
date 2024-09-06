// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using System.Text;
using Serilog;

namespace xcsync.Workers;

readonly record struct FileMessage (string Id, string Path, string Content);

readonly record struct CopyFileMessage (string Id, string sourcePath, string destinationPath);

class FileWorker (ILogger Logger, IFileSystem fileSystem) : BaseWorker<FileMessage> {
	public override async Task ConsumeAsync (FileMessage message, CancellationToken cancellationToken = default)
	{
		try {
			// Read the existing file and detect the BOM
			byte [] preamble = [];
			Encoding encoding = Encoding.Default;

			if (fileSystem.File.Exists (message.Path)) {

				byte [] bom = new byte [4];
				using (var fs = fileSystem.FileStream.New (message.Path, FileMode.Open, FileAccess.Read)) {
					fs.Read (bom, 0, 4);
				}

				using var inStream = fileSystem.FileStream.New (message.Path, FileMode.Open, FileAccess.Read);
				using var reader = new StreamReader (inStream, true);
				await reader.ReadToEndAsync (cancellationToken);
				encoding = reader.CurrentEncoding;
				var encodingPreamble = encoding.GetPreamble ();
				preamble = encodingPreamble.SequenceEqual (bom [0..encodingPreamble.Length]) ? encodingPreamble : preamble;
			}

			// Write the new content to the file, using the detected BOM
			using var vStream = fileSystem.File.Create (message.Path);

			// Writes the preamble first
			vStream.Write (preamble, 0, preamble.Length);

			// Gets the bytes from text
			byte [] data = encoding.GetBytes (message.Content);
			vStream.Write (data, 0, data.Length);
			vStream.Close ();

		} catch (Exception ex) {
			Logger?.Fatal ($"Exception in ConsumeAsync: {ex.Message}");
			throw;
		}
	}

	public override Task ConsumeAsync (FileMessage message, Exception exception, CancellationToken token = default)
	{
		Log.Error (exception, "Error processing file message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
