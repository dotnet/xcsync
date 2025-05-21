// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace xcsync.Commands;

static class SharedOptions {
	public static readonly Option<Verbosity> Verbose =
		new ("Verbosity", ["--verbosity", "-v"]) {
			Description = Strings.Options.VerbosityDescription,
			DefaultValueFactory = (p) => Verbosity.Normal,
			Required = false,
			Arity = ArgumentArity.ZeroOrOne,
		};

	public static readonly Option<string> DotnetPath =
		new ("DotNetPath", ["--dotnet-path", "-d"]) {
			Description = Strings.Options.DotnetPathDescription,
			DefaultValueFactory = (p) => string.Empty,
			Required = false,
			Arity = ArgumentArity.ZeroOrOne,
		};
}
