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
	public async Task ConsumeAsync_ExistingFile_KeepsEncoding (Encoding encoding)
	{

		// Arrange
		var fileSystem = new MockFileSystem ();
		var logger = Mock.Of<ILogger> ();
		var fileWorker = new FileWorker (logger, fileSystem);

		await fileSystem.File.WriteAllTextAsync (fileName, fileContent, encoding);

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

	[Fact]
	public async Task ConsumeAsync_NewFile_UsesDefaultEncodingAndPreamble ()
	{
		// Arrange
		var fileSystem = new MockFileSystem ();
		var logger = Mock.Of<ILogger> ();
		var fileWorker = new FileWorker (logger, fileSystem);
		var fileWorker_defaultEncoding = new UTF8Encoding (true);
		var fileWorker_defaultPreamble = fileWorker_defaultEncoding.GetPreamble ();

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

		Assert.Equivalent (fileWorker_defaultEncoding.EncodingName, actualEncoding.EncodingName);
		Assert.Equal (fileWorker_defaultPreamble, actualPreamble);
	}

}
