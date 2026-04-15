// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;

namespace xcsync.tests;

public class ContinuousSyncContextTest {

	[Fact]
	public void ChangeMessage_DefaultsIncrementalToFalse ()
	{
		var message = new ChangeMessage ("1", "/tmp/Test.cs", SyncDirection.ToXcode, CreateMonitor (), CreateMonitor ());

		Assert.False (message.Incremental);
	}

	[Fact]
	public void ChangeMessage_CanEnableIncremental ()
	{
		var message = new ChangeMessage ("1", "/tmp/Test.cs", SyncDirection.ToXcode, CreateMonitor (), CreateMonitor (), true);

		Assert.True (message.Incremental);
	}

	static ProjectFileChangeMonitor CreateMonitor () =>
		new (new MockFileSystem (), Mock.Of<IFileSystemWatcher> (), Mock.Of<ILogger> ());
}
