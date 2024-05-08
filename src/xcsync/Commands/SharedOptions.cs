// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;

namespace xcsync.Commands;

public static class SharedOptions {
	public static readonly Option<Verbosity> Verbose =
		new (["--verbosity", "-v"],
			getDefaultValue: () => Verbosity.Normal) {
			IsRequired = false,
			Arity = ArgumentArity.ZeroOrOne,
			Description = "Verbosity setting."
		};
}
