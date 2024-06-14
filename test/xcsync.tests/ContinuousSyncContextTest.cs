// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Marille;
using Moq;
using Serilog;
using xcsync.Projects;

namespace xcsync.tests; 

public class ContinuousSyncContextTest {
	private Mock<IHub> mockHub = new();
	private ContinuousSyncContext context = new(Mock.Of<IFileSystem> (), Mock.Of<ITypeService> (), "projectPath",
		"targetDir", "framework", Mock.Of<ILogger> ());

	[Fact]
	public async Task SyncChannel_AddsSyncWorkerToWorkersList()
	{
		await context.SyncChannel("Sync", mockHub.Object);
		Assert.Contains (context.workers, w => w.Item1 is SyncWorker);
		mockHub.Verify(h => h.RegisterAsync(It.IsAny<string>(), It.IsAny<SyncWorker>()), Times.Once);
		mockHub.Verify(h => h.Publish(It.IsAny<string>(), It.IsAny<SyncMessage>()), Times.Once);
	}
	
	[Fact]
	public async Task ErrorChannel_AddsErrorWorkerToWorkersList()
	{
		await context.ErrorChannel("Error", new Exception(), mockHub.Object);
		Assert.Contains (context.workers, w => w.Item1 is ErrorWorker);
		mockHub.Verify(h => h.RegisterAsync(It.IsAny<string>(), It.IsAny<ErrorWorker>()), Times.Once);
		mockHub.Verify(h => h.Publish(It.IsAny<string>(), It.IsAny<ErrorMessage>()), Times.Once);
	}
}
