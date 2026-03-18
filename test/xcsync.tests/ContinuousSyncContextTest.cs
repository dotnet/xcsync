// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Moq;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync.tests;

public class ContinuousSyncContextTest {
	readonly Mock<IHub> mockHub = new ();
	readonly ContinuousSyncContext context = new (Mock.Of<IFileSystem> (), Mock.Of<ITypeService> (), "projectPath",
		"targetDir", "framework", Mock.Of<ILogger> ());
}
