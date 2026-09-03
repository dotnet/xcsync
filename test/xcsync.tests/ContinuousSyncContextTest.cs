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

	[Fact]
	public void ChangeMessage_DefaultsExplicitTypesToFalse ()
	{
		var message = new ChangeMessage ("1", "/tmp/Test.cs", SyncDirection.FromXcode, CreateMonitor (), CreateMonitor ());

		Assert.False (message.ExplicitTypes);
	}

	[Fact]
	public void ChangeMessage_CanEnableExplicitTypes ()
	{
		var message = new ChangeMessage ("1", "/tmp/Test.cs", SyncDirection.FromXcode, CreateMonitor (), CreateMonitor (), ExplicitTypes: true);

		Assert.True (message.ExplicitTypes);
	}

	static ProjectFileChangeMonitor CreateMonitor () =>
		new (new MockFileSystem (), Mock.Of<IFileSystemWatcher> (), Mock.Of<ILogger> ());
}
