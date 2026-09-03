// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace xcsync.Commands;

static class SharedOptions {
	public static readonly Option<Verbosity> Verbose =
		new ("--verbosity", "-v") {
			Arity = ArgumentArity.ZeroOrOne,
			Description = Strings.Options.VerbosityDescription,
			DefaultValueFactory = _ => Verbosity.Normal,
			Recursive = true,
		};

	public static readonly Option<string> DotnetPath =
		new ("--dotnet-path", "-d") {
			Arity = ArgumentArity.ZeroOrOne,
			Description = Strings.Options.DotnetPathDescription,
			DefaultValueFactory = _ => string.Empty,
			Recursive = true,
		};
}
