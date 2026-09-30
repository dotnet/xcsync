// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin.MacDev;
using xcsync.Projects.Xcode;

namespace xcsync.tests.Projects.Xcode;

public class PbxGuidTest {
	[Fact]
	public void PbxGuid_EmptyGuid_IsEmpty ()
	{
		var emptyGuid = PbxGuid.Empty;
		Assert.Equal (PbxGuid.Empty, emptyGuid);
	}

	[Fact]
	public void PbxGuid_NewGuid_IsNotEmpty ()
	{
		var newGuid = PbxGuid.NewGuid ();
		Assert.NotEqual (PbxGuid.Empty, newGuid);
	}

	[Fact]
	public void PbxGuid_FromString_Valid24CharString ()
	{
		var guidString = "1234567890ABCDEF12345678";
		var pbxGuid = new PbxGuid (guidString);
		Assert.Equal (guidString, pbxGuid.ToString ());
	}

	[Fact]
	public void PbxGuid_FromString_Valid32CharString ()
	{
		var guidString = "1234567890ABCDEF1234567890ABCDEF";
		var pbxGuid = new PbxGuid (guidString);
		Assert.Equal (guidString, pbxGuid.ToString ());
	}

	[Fact]
	public void PbxGuid_FromString_InvalidLength_ThrowsException ()
	{
		Assert.Throws<ArgumentOutOfRangeException> (() => new PbxGuid ("123456"));
	}

	[Fact]
	public void PbxGuid_Equals_SameGuids_ReturnsTrue ()
	{
		var guidString = "1234567890ABCDEF12345678";
		var pbxGuid1 = new PbxGuid (guidString);
		var pbxGuid2 = new PbxGuid (guidString);
		Assert.True (pbxGuid1.Equals (pbxGuid2));
	}

	[Fact]
	public void PbxGuid_Equals_DifferentGuids_ReturnsFalse ()
	{
		var pbxGuid1 = PbxGuid.NewGuid ();
		var pbxGuid2 = PbxGuid.NewGuid ();
		Assert.False (pbxGuid1.Equals (pbxGuid2));
	}

	[Fact]
	public void PbxGuid_GetHashCode_SameGuids_ReturnsSameHashCode ()
	{
		var guidString = "1234567890ABCDEF12345678";
		var pbxGuid1 = new PbxGuid (guidString);
		var pbxGuid2 = new PbxGuid (guidString);
		Assert.Equal (pbxGuid1.GetHashCode (), pbxGuid2.GetHashCode ());
	}

	[Fact]
	public void PbxGuid_ImplicitConversion_FromString ()
	{
		PbxGuid pbxGuid = "1234567890ABCDEF12345678";
		Assert.Equal ("1234567890ABCDEF12345678", pbxGuid.ToString ());
	}

	[Fact]
	public void PbxGuid_ImplicitConversion_ToPString ()
	{
		var pbxGuid = new PbxGuid ("1234567890ABCDEF12345678");
		PString pString = pbxGuid;
		Assert.Equal ("1234567890ABCDEF12345678", pString.Value);
	}

	[Fact]
	public void PbxGuid_ExplicitConversion_FromPObject ()
	{
		var pString = new PString ("1234567890ABCDEF12345678");
		var pbxGuid = (PbxGuid) pString;
		Assert.Equal ("1234567890ABCDEF12345678", pbxGuid.ToString ());
	}

	[Fact]
	public void PbxGuid_EqualityOperator_SameGuids_ReturnsTrue ()
	{
		var guidString = "1234567890ABCDEF12345678";
		var pbxGuid1 = new PbxGuid (guidString);
		var pbxGuid2 = new PbxGuid (guidString);
		Assert.True (pbxGuid1 == pbxGuid2);
	}

	[Fact]
	public void PbxGuid_InequalityOperator_DifferentGuids_ReturnsTrue ()
	{
		var pbxGuid1 = PbxGuid.NewGuid ();
		var pbxGuid2 = PbxGuid.NewGuid ();
		Assert.True (pbxGuid1 != pbxGuid2);
	}
}
