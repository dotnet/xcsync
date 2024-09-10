// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace xcsync.tests;

public class ScriptsTests {
	
	[MacOSOnlyFact]
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
