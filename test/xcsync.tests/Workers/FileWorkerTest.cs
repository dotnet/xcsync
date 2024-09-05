// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Moq;
using Serilog;
using xcsync.Workers;

namespace xcsync.tests.Workers;

public class FileWorkerTests () : Base {

	public static IEnumerable<object[]> Encodings => 
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
	public async Task WritingNewContent_KeepsEncodingFromExistingFile (Encoding encoding) {

		// Arrange
		var fileSystem = new MockFileSystem ();
		var logger = Mock.Of<ILogger> ();
		var fileWorker = new FileWorker (logger, fileSystem );

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
		var actualPreamble = DetectPreamble (bom);
		inStream.Seek (0, SeekOrigin.Begin);

		using var reader = new StreamReader (inStream, true);
		reader.ReadToEnd ();
		var actualEncoding = reader.CurrentEncoding;

		Assert.Equivalent (encoding.EncodingName, actualEncoding.EncodingName);
		Assert.Equal (encoding.GetPreamble(), actualPreamble);
	}

	static byte [] DetectPreamble (byte [] bom)
	{
		if (bom [0] == 0x2b && bom [1] == 0x2f && bom [2] == 0x76) return bom [0..3];
		if (bom [0] == 0xef && bom [1] == 0xbb && bom [2] == 0xbf) return bom [0..3];
		if (bom [0] == 0xff && bom [1] == 0xfe) return bom [0..2]; // UTF-16LE
		if (bom [0] == 0xfe && bom [1] == 0xff) return bom [0..2]; // UTF-16BE
		if (bom [0] == 0x00 && bom [1] == 0x00 && bom [2] == 0xfe && bom [3] == 0xff) return bom [0..4];
		return [];
	}
}