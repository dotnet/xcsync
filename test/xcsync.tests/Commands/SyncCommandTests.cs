// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using xcsync.Commands;

namespace xcsync.tests.Commands;

public class SyncCommandTests {

	[Fact]
	public void SyncCommand_AddsExplicitTypesOption_WithDefaultFalse ()
	{
		var command = new SyncCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var explicitTypes = GetExplicitTypesOption (command);
		var parseResult = command.Parse ([]);

		Assert.Equal (Strings.Options.ExplicitTypesDescription, explicitTypes.Description);
		Assert.Contains ("-e", explicitTypes.Aliases);
		Assert.False (parseResult.CommandResult.GetValueForOption (explicitTypes));
	}

	[Theory]
	[InlineData ("--explicit-types")]
	[InlineData ("-e")]
	public void SyncCommand_ParsesExplicitTypesAliases (string alias)
	{
		var command = new SyncCommand (new MockFileSystem (), Mock.Of<ILogger> ());

		var explicitTypes = GetExplicitTypesOption (command);
		var parseResult = command.Parse ([alias]);

		Assert.True (parseResult.CommandResult.GetValueForOption (explicitTypes));
	}

	static Option<bool> GetExplicitTypesOption (SyncCommand command) =>
		Assert.IsType<Option<bool>> (Assert.Single (command.Options.Where (option => option.Aliases.Contains ("--explicit-types"))));
}
