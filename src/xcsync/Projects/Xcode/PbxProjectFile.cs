// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin.MacDev;

namespace xcsync.Projects.Xcode.Model;

record class PbxProjectFile (FilePath Filename) {
	readonly PDictionary properties = [];
	readonly List<PbxObject> objects = [];
	readonly Dictionary<PbxGuid, PbxObject> objectGuidMap = [];

	public IReadOnlyList<PbxObject> Objects {
		get { return objects; }
	}

	public PbxProject? RootObject {
		get { return properties.GetPbxObject<PbxProject> (this, "rootObject"); }
	}

	public int ArchiveVersion {
		get { return properties.GetInt32 ("archiveVersion"); }
		set { properties.Set ("archiveVersion", value); }
	}

	public int ObjectVersion {
		get { return properties.GetInt32 ("objectVersion"); }
		set { properties.Set ("objectVersion", value); }
	}

	public void AddObject (PbxObject obj)
	{
		ArgumentNullException.ThrowIfNull (obj, nameof (obj));

		if (objectGuidMap.ContainsKey (obj.Guid))
			throw new ArgumentException ("object is already registered", nameof (obj));

		lock (objects) {
			objects.Add (obj);
			objectGuidMap.Add (obj.Guid, obj);
		}
	}

	public bool RemoveObject (PbxObject obj)
	{
		ArgumentNullException.ThrowIfNull (obj, nameof (obj));

		lock (objects) {
			var mapRemove = objectGuidMap.Remove (obj.Guid);
			var listRemove = objects.Remove (obj);
			if (mapRemove != listRemove)
				throw new Exception ("object retention inconsistency; should not be reached!");
			return mapRemove;
		}
	}

	public T? GetObject<T> (PbxGuid guid) where T : PbxObject
	{
		lock (objects) {
			objectGuidMap.TryGetValue (guid, out PbxObject? obj);
			return obj as T;
		}
	}
}
