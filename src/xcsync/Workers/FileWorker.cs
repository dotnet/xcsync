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
			// Preserve the preamble and encoding from the existing file
			Encoding encoding = new UTF8Encoding (true);
			byte [] preamble = [];

			// If the file exists, get the encoding and preamble from the file to preserve it
			if (fileSystem.File.Exists (message.Path)) {

				byte [] bom = new byte [4];
				using var inStream = fileSystem.FileStream.New (message.Path, FileMode.Open, FileAccess.Read);
				inStream.Read (bom, 0, 4);
				inStream.Seek (0, SeekOrigin.Begin);

				using var reader = new StreamReader (inStream, true);
				await reader.ReadToEndAsync (cancellationToken);
				encoding = reader.CurrentEncoding;
				var encodingPreamble = encoding.GetPreamble ();
				// When StreamReader detects the encoding, it is suppoosed to detect the presence of the BOM
				// However in testing GetPreamble seems to always return the BOM for the encoding regardless of the BOM
				// actually being present in the file. So we check the encoding's preamble vs the actual preamble,
				// and if they match, then the BOM was actually present in the file.
				preamble = encodingPreamble.SequenceEqual (bom [0..encodingPreamble.Length]) ? encodingPreamble : preamble;
			} else {
				// if the file doesn't exist, use the defauilt encoding preamble
				preamble = encoding.GetPreamble ();
			}

			// Write the new content to the file, using the detected BOM
			using var outStream = fileSystem.File.Create (message.Path);

			// Writes the preamble first
			outStream.Write (preamble, 0, preamble.Length);

			// Gets the bytes from text
			byte [] data = encoding.GetBytes (message.Content);
			outStream.Write (data, 0, data.Length);

		} catch (Exception ex) {
			Logger?.Fatal (ex, $"Exception in ConsumeAsync: {ex.Message}");
			throw;
		}
	}

	public override Task ConsumeAsync (FileMessage message, Exception exception, CancellationToken token = default)
	{
		Log.Error (exception, "Error processing file message {Id}", message.Id);
		return Task.CompletedTask;
	}
}
