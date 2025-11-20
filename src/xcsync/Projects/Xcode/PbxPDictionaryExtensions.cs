// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin.MacDev;
using xcsync.Projects.Xcode.Model;

namespace xcsync.Projects.Xcode;

static class PBXPListExtensions {
	static void AssertPListWithKey (PObjectContainer plist, string key)
	{
		ArgumentNullException.ThrowIfNull (plist, nameof (plist));

		ArgumentNullException.ThrowIfNull (key, nameof (key));
	}

	public static PArray SetArrayValues (this PArray array, string [] values)
	{
		ArgumentNullException.ThrowIfNull (array, nameof (array));

		foreach (var value in values)
			array.Add (value);

		return array;
	}

	public static PArray SetArrayValues (this PArray array, FilePath [] values)
	{
		ArgumentNullException.ThrowIfNull (array, nameof (array));

		foreach (var value in values)
			array.Add ((string) value);

		return array;
	}

	public static FilePath [] ToFilePathArray (this PArray array)
	{
		return array.Cast<PString> ().Select (s => (FilePath) s.Value).ToArray ();
	}

	public static void Set (this PDictionary plist, string key, int value)
	{
		AssertPListWithKey (plist, key);

		var result = plist.Get<PNumber> (key);
		if (result == null)
			plist [key] = new PNumber (value);
		else
			result.Value = value;
	}

	public static void Set (this PDictionary plist, string key, uint value)
	{
		Set (plist, key, (int) value);
	}

	public static void Set (this PDictionary plist, string key, string? value)
	{
		ArgumentNullException.ThrowIfNull (value, nameof (value));

		AssertPListWithKey (plist, key);

		var result = plist.Get<PString> (key);
		if (result == null)
			plist [key] = new PString (value);
		else
			result.Value = value;
	}

	public static string? GetString (this PDictionary plist, string key)
	{
		AssertPListWithKey (plist, key);
		return plist.Get<PString> (key)?.Value;
	}

	public static int GetInt32 (this PDictionary plist, string key)
	{
		AssertPListWithKey (plist, key);
		var value = plist.Get<PNumber> (key);
		return value == null ? 0 : (int) value.Value;
	}

	public static uint GetUInt32 (this PDictionary plist, string key) => (uint) GetInt32 (plist, key);

	public static T? GetPbxObject<T> (this PDictionary plist, PbxProjectFile projectFile, string key) where T : PbxObject
	{
		AssertPListWithKey (plist, key);

		ArgumentNullException.ThrowIfNull (projectFile, nameof (projectFile));

		key = plist.GetString (key).Value;
		if (string.IsNullOrEmpty (key))
			return null;

		return projectFile.GetObject<T> (new PbxGuid (key));
	}

	public static T? GetPbxObject<T> (this PDictionary plist, PbxObject parent, string key) where T : PbxObject
	{
		AssertPListWithKey (plist, key);

		ArgumentNullException.ThrowIfNull (parent, nameof (parent));

		if (parent.ProjectFile == null)
			throw new ArgumentException ("parent object's ProjectFile must be non-null", nameof (parent));

		return parent.ProjectFile.GetObject<T> (
			new PbxGuid (
				plist.GetString (key).Value
			)
		);
	}

	public static void SetPbxObjectRef (this PDictionary plist, string key, PbxObject? obj)
	{
		AssertPListWithKey (plist, key);

		if (obj == null)
			plist.Remove (key);
		else
			plist.SetString (key, obj.Guid.ToString ());
	}
}

