// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Marille;
using Moq;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;

namespace xcsync.tests; 

public class ContinuousSyncContextTest {
	private Mock<IHub> mockHub = new();
	private ContinuousSyncContext context = new(Mock.Of<IFileSystem> (), Mock.Of<ITypeService> (), "projectPath",
		"targetDir", "framework", Mock.Of<ILogger> ());

	[Fact]
	public async Task ChangeWorker_Sync()
	{
		await context.SyncChange("path", mockHub.Object);
		Assert.Contains (context.workers, w => w.Item1 is ChangeWorker);
		mockHub.Verify(h => h.RegisterAsync(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.IsAny<ChangeWorker>()), Times.Once);
		mockHub.Verify(h => h.Publish(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage>(m => m.Payload is SyncLoad)), Times.Once);
	}
	
	[Fact]
	public async Task ChangeWorker_Error()
	{
		// can't make this a theory cuz of diff # of params :/
		await context.SyncError("path", new Exception(), mockHub.Object);
		Assert.Contains (context.workers, w => w.Item1 is ChangeWorker);
		mockHub.Verify(h => h.RegisterAsync(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.IsAny<ChangeWorker>()), Times.Once);
		mockHub.Verify(h => h.Publish(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage>(m => m.Payload is ErrorLoad)), Times.Once);
	}

	[Fact]
	public async Task ChangeWorker_Rename()
	{
		await context.SyncRename("path", mockHub.Object);
		Assert.Contains (context.workers, w => w.Item1 is ChangeWorker);
		mockHub.Verify(h => h.RegisterAsync(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.IsAny<ChangeWorker>()), Times.Once);
		mockHub.Verify(h => h.Publish(It.Is<string>(s => s == ContinuousSyncContext.ChangeChannel), It.Is<ChangeMessage>(m => m.Payload is RenameLoad)), Times.Once);
	}
}
