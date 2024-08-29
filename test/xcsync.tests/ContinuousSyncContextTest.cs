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

	// [Fact]
	// public async Task ChangeWorker_Sync ()
	// {
	// 	await context.SyncChange ("path", mockHub.Object);
	// 	mockHub.Verify (h => h.PublishAsync (It.Is<string> (s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage> (m => m.Change is SyncLoad)), Times.Once);
	// }

	// [Fact]
	// public async Task ChangeWorker_Error ()
	// {
	// 	// can't make this a theory cuz of diff # of params :/
	// 	await context.SyncError ("path", new Exception (), mockHub.Object);
	// 	mockHub.Verify (h => h.PublishAsync (It.Is<string> (s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage> (m => m.Change is ErrorLoad)), Times.Once);
	// }

	// [Fact]
	// public async Task ChangeWorker_Rename ()
	// {
	// 	await context.SyncRename ("path", mockHub.Object);
	// 	mockHub.Verify (h => h.PublishAsync (It.Is<string> (s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage> (m => m.Change is RenameLoad)), Times.Once);
	// }
}
