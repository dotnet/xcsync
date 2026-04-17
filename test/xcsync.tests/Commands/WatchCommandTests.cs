// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using xcsync.Commands;

namespace xcsync.tests.Commands;

public class WatchCommandTests {

	[Fact]
	public void WatchCommand_AddsIncrementalOption_WithDefaultFalse ()
	{
		var command = new WatchCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var incremental = GetIncrementalOption (command);
		var parseResult = command.Parse ([]);

		Assert.Equal (Strings.Options.IncrementalDescription, incremental.Description);
		Assert.Contains ("-i", incremental.Aliases);
		Assert.False (parseResult.CommandResult.GetValueForOption (incremental));
	}

	[Fact]
	public void WatchCommand_AddsExplicitTypesOption_WithDefaultFalse ()
	{
		var command = new WatchCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var explicitTypes = GetExplicitTypesOption (command);
		var parseResult = command.Parse ([]);

		Assert.Equal (Strings.Options.ExplicitTypesDescription, explicitTypes.Description);
		Assert.Contains ("-e", explicitTypes.Aliases);
		Assert.False (parseResult.CommandResult.GetValueForOption (explicitTypes));
	}

	[Theory]
	[InlineData ("--incremental")]
	[InlineData ("-i")]
	public void WatchCommand_ParsesIncrementalAliases (string alias)
	{
		var command = new WatchCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var incremental = GetIncrementalOption (command);
		var parseResult = command.Parse ([alias]);

		Assert.True (parseResult.CommandResult.GetValueForOption (incremental));
	}

	[Theory]
	[InlineData ("--explicit-types")]
	[InlineData ("-e")]
	public void WatchCommand_ParsesExplicitTypesAliases (string alias)
	{
		var command = new WatchCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var explicitTypes = GetExplicitTypesOption (command);
		var parseResult = command.Parse ([alias]);

		Assert.True (parseResult.CommandResult.GetValueForOption (explicitTypes));
	}

	static Option<bool> GetIncrementalOption (WatchCommand command) =>
		Assert.IsType<Option<bool>> (Assert.Single (command.Options.Where (option => option.Aliases.Contains ("--incremental"))));

	static Option<bool> GetExplicitTypesOption (WatchCommand command) =>
		Assert.IsType<Option<bool>> (Assert.Single (command.Options.Where (option => option.Aliases.Contains ("--explicit-types"))));
}
