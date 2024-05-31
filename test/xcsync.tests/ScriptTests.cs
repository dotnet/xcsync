// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.tests;

public class ScriptsTests {
	[Fact]
	public void SelectXcode_ReturnsXcodePath ()
	{
		// Act
		string xcodePath = Scripts.SelectXcode ();

		// Assert
		Assert.NotNull (xcodePath);
		Assert.NotEmpty (xcodePath);
		Assert.EndsWith (".app", xcodePath);
	}
}