// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Moq;
using Serilog;
using xcsync.Workers;

namespace xcsync.tests.Workers;

public class FileWorkerTests () : Base {

	public static IEnumerable<object []> Encodings =>
	[
		[new UTF8Encoding (false)],
		[new UTF8Encoding (true)],
	];

	const string fileName = "test.cs";
	const string fileContent = @"// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//


[Register (""ViewController"")]
partial class ViewController {

	void ReleaseDesignerOutlets ()
	{
	}
}
";

	[Theory]
	[MemberData (nameof (Encodings))]
	public async Task WritingNewContent_KeepsEncodingFromExistingFile (Encoding encoding)
	{

		// Arrange
		var fileSystem = new MockFileSystem ();
		var logger = Mock.Of<ILogger> ();
		var fileWorker = new FileWorker (logger, fileSystem);

		using (var vStream = fileSystem.File.Create (fileName)) {
			// Gets the preamble in order to attach the BOM
			var vPreambleByte = encoding.GetPreamble ();

			// Writes the preamble first
			vStream.Write (vPreambleByte, 0, vPreambleByte.Length);

			// Gets the bytes from text
			byte [] vByteData = encoding.GetBytes (fileContent);
			vStream.Write (vByteData, 0, vByteData.Length);
			vStream.Close ();
		}

		// Act
		await fileWorker.ConsumeAsync (new FileMessage ("0", fileName, fileContent), default);

		// Assert
		Assert.True (fileSystem.File.Exists (fileName));

		using var inStream = fileSystem.FileStream.New (fileName, FileMode.Open, FileAccess.Read);
		byte [] bom = new byte [4];
		inStream.Read (bom, 0, 4);
		inStream.Seek (0, SeekOrigin.Begin);

		using var reader = new StreamReader (inStream, true);
		reader.ReadToEnd ();
		var actualEncoding = reader.CurrentEncoding;
		var encodingPreamble = actualEncoding.GetPreamble ();
		var actualPreamble = encodingPreamble.SequenceEqual (bom [0..encodingPreamble.Length]) ? encodingPreamble : [];

		Assert.Equivalent (encoding.EncodingName, actualEncoding.EncodingName);

		Assert.Equal (encoding.GetPreamble (), actualPreamble);
	}
}
