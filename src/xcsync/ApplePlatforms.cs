// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections;
using Xamarin;

namespace xcsync;

class ApplePlatforms : IReadOnlyDictionary<string, (Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion)> {
	static readonly Dictionary<string, (Frameworks, string, string)> applePlatforms = new ()
		{
			{ "ios", (Frameworks.GetiOSFrameworks(false), SdkVersions.DotNetMiniOS, SdkVersions.iOS) },
			{ "maccatalyst", (Frameworks.GetMacCatalystFrameworks(), SdkVersions.DotNetMinMacCatalyst, SdkVersions.MacCatalyst) },
			{ "macos", (Frameworks.MacFrameworks, SdkVersions.DotNetMinOSX, SdkVersions.OSX) },
			{ "tvos", (Frameworks.TVOSFrameworks, SdkVersions.DotNetMinTVOS, SdkVersions.TVOS) }
		};

	public IEnumerable<string> Keys => applePlatforms.Keys;

	public IEnumerable<(Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion)> Values => applePlatforms.Values;

	public int Count => applePlatforms.Count;

	public (Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion) this [string key] => applePlatforms [key];

	public bool ContainsKey (string key) => applePlatforms.ContainsKey (key);

	public IEnumerator<KeyValuePair<string, (Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion)>> GetEnumerator () => applePlatforms.GetEnumerator ();

	public bool TryGetValue (string key, out (Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion) value) => applePlatforms.TryGetValue (key, out value);

	IEnumerator IEnumerable.GetEnumerator () => applePlatforms.GetEnumerator ();
}
