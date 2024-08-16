// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;

namespace xcsync.Commands;

static class SharedOptions {
	public static readonly Option<Verbosity> Verbose =
		new (["--verbosity", "-v"],
			getDefaultValue: () => Verbosity.Normal) {
			IsRequired = false,
			Arity = ArgumentArity.ZeroOrOne,
			Description = Strings.Options.VerbosityDescription
		};

	public static readonly Option<string> DotnetPath =
		new (["--dotnet-path", "-d"],
			getDefaultValue: () => string.Empty) {
			IsRequired = false,
			Arity = ArgumentArity.ZeroOrOne,
			Description = Strings.Options.DotnetPathDescription
		};
}
