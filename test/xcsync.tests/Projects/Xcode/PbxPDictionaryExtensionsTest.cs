// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin.MacDev;
using xcsync.Projects.Xcode;
using xcsync.Projects.Xcode.Model;

namespace xcsync.tests.Projects.Xcode;

public class PBXPListExtensionsTest {
	const string GuidValue1 = "1234567890ABCDEF1234567890ABCDEF";
	const string GuidValue2 = "ABCDEF1234567890ABCDEF1234567890";

	[Fact]
	public void SetArrayValues_WithStringArray_SetsValues ()
	{
		var array = new PArray ();
		var values = new [] { "value1", "value2" };

		array.SetArrayValues (values);

		Assert.Equal (values.Length, array.Count);
		Assert.Contains (array, item => ((PString) item).Value == "value1");
		Assert.Contains (array, item => ((PString) item).Value == "value2");
	}

	[Fact]
	public void SetArrayValues_WithFilePathArray_SetsValues ()
	{
		var array = new PArray ();
		var values = new [] { new FilePath ("/path1"), new FilePath ("/path2") };

		array.SetArrayValues (values);

		Assert.Equal (values.Length, array.Count);
		Assert.Contains (array, item => ((PString) item).Value == "/path1");
		Assert.Contains (array, item => ((PString) item).Value == "/path2");
	}

	[Fact]
	public void ToFilePathArray_ConvertsToFilePathArray ()
	{
		var array = new PArray { new PString ("/path1"), new PString ("/path2") };

		var result = array.ToFilePathArray ();

		Assert.Equal (2, result.Length);
		Assert.Contains (result, item => item.ToString () == "/path1");
		Assert.Contains (result, item => item.ToString () == "/path2");
	}

	[Fact]
	public void Set_WithIntValue_SetsValue ()
	{
		var plist = new PDictionary ();
		plist.Set ("key", 42);

		var result = plist.Get<PNumber> ("key");

		Assert.NotNull (result);
		Assert.Equal (42, result.Value);
	}

	[Fact]
	public void Set_WithUIntValue_SetsValue ()
	{
		var plist = new PDictionary ();
		plist.Set ("key", (uint) 42);

		var result = plist.Get<PNumber> ("key");

		Assert.NotNull (result);
		Assert.Equal (42, result.Value);
	}

	[Fact]
	public void Set_WithStringValue_SetsValue ()
	{
		var plist = new PDictionary ();
		plist.Set ("key", "value");

		var result = plist.Get<PString> ("key");

		Assert.NotNull (result);
		Assert.Equal ("value", result.Value);
	}

	[Fact]
	public void GetString_ReturnsCorrectValue ()
	{
		var plist = new PDictionary { { "key", new PString ("value") } };

		var result = plist.GetString ("key");

		Assert.Equal ("value", result);
	}

	[Fact]
	public void GetInt32_ReturnsCorrectValue ()
	{
		var plist = new PDictionary { { "key", new PNumber (42) } };

		var result = plist.GetInt32 ("key");

		Assert.Equal (42, result);
	}

	[Fact]
	public void GetUInt32_ReturnsCorrectValue ()
	{
		var plist = new PDictionary { { "key", new PNumber (42) } };

		var result = plist.GetUInt32 ("key");

		Assert.Equal ((uint) 42, result);
	}

	[Fact]
	public void GetPbxObject_WithProjectFile_ReturnsCorrectObject ()
	{
		var projectFile = new PbxProjectFile ("");
		var obj = new PbxObject (projectFile, new PbxGuid (GuidValue1), []);
		projectFile.AddObject (obj);

		var plist = new PDictionary { { "key", new PString (GuidValue1) } };

		var result = plist.GetPbxObject<PbxObject> (projectFile, "key");

		Assert.NotNull (result);
		Assert.Equal (obj, result);
	}

	[Fact]
	public void GetPbxObject_WithParent_ReturnsCorrectObject ()
	{
		var projectFile = new PbxProjectFile ("");
		var parent = new PbxObject (projectFile, new PbxGuid (GuidValue2), []);
		var obj = new PbxObject (projectFile, new PbxGuid (GuidValue1), []);
		projectFile.AddObject (obj);

		var plist = new PDictionary { { "key", new PString (GuidValue1) } };

		var result = plist.GetPbxObject<PbxObject> (parent, "key");

		Assert.NotNull (result);
		Assert.Equal (obj, result);
	}

	[Fact]
	public void SetPbxObjectRef_SetsCorrectReference ()
	{
		var plist = new PDictionary ();
		var projectFile = new PbxProjectFile ("");
		var obj = new PbxObject (projectFile, new PbxGuid (GuidValue1), []);

		plist.SetPbxObjectRef ("key", obj);

		var result = plist.GetString ("key");

		Assert.Equal (GuidValue1, result);
	}

	[Fact]
	public void SetPbxObjectRef_RemovesReferenceWhenNull ()
	{
		var plist = new PDictionary { { "key", new PString (GuidValue1) } };

		plist.SetPbxObjectRef ("key", null);

		Assert.False (plist.ContainsKey ("key"));
	}
}
