// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

class XcodeProject {
	[JsonPropertyName ("classes")]
	public Dictionary<string, object>? Classes { get; set; }

	[JsonPropertyName ("objectVersion")]
	public string? ObjectVersion { get; set; }

	[JsonPropertyName ("archiveVersion")]
	public string? ArchiveVersion { get; set; }

	[JsonConverter (typeof (XcodeProjectObjectsJsonConverter))]
	[JsonPropertyName ("objects")]
	public IDictionary<string, XcodeObject> Objects { get; set; } = new Dictionary<string, XcodeObject> ();

	[JsonPropertyName ("rootObject")]
	public string? RootObject { get; set; }
}

[JsonConverter (typeof (XcodeObjectConverter))]
class XcodeObject {

	[JsonPropertyName ("isa")]
	[JsonPropertyOrder (-1)] // ISA should be the first property in the JSON object
	public string? Isa { get; set; }

	string? token;

	[JsonIgnore]
	public string Token {
		get {
			if (string.IsNullOrEmpty (token)) {

				int hc = GetHashCode ();
				token = hc.ToString ("X").PadRight (24, '0');
			}
			return token;
		}
		set {
			token = value;
		}
	}
}

class PBXResourcesBuildPhase : XcodeObject {

	[JsonPropertyName ("buildActionMask")]
	public string BuildActionMask { get; set; } = int.MaxValue.ToString ();

	[JsonPropertyName ("files")]
	public List<string>? Files { get; set; }

	[JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
	public string RunOnlyForDeploymentPostprocessing { get; set; } = "0";
}

class XCBuildConfiguration : XcodeObject {

	[JsonPropertyName ("buildSettings")]
	[JsonConverter (typeof (BuildSettingsConverter))]
	public IDictionary<string, IList<string>>? BuildSettings { get; set; }

	[JsonPropertyName ("name")]
	public string? Name { get; set; }
}

class PBXFileReference : XcodeObject {

	[JsonPropertyName ("path")]
	public string? Path { get; set; }

	[JsonPropertyName ("includeInIndex")]
	public string? IncludeInIndex { get; set; }

	[JsonPropertyName ("name")]
	public string? Name { get; set; }

	[JsonPropertyName ("explicitFileType")]
	public string? ExplicitFileType { get; set; }

	[JsonPropertyName ("lastKnownFileType")]
	public string? LastKnownFileType { get; set; }

	[JsonPropertyName ("sourceTree")]
	public string? SourceTree { get; set; }
}

class PBXBuildFile : XcodeObject {

	[JsonPropertyName ("fileRef")]
	public string? FileRef { get; set; }
}

class PBXFrameworksBuildPhase : XcodeObject {

	[JsonPropertyName ("buildActionMask")]
	public string BuildActionMask { get; set; } = int.MaxValue.ToString ();

	[JsonPropertyName ("files")]
	public List<string>? Files { get; set; }

	[JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
	public string RunOnlyForDeploymentPostprocessing { get; set; } = "0";
}

class PBXProject : XcodeObject {

	[JsonPropertyName ("buildConfigurationList")]
	public string? BuildConfigurationList { get; set; }

	[JsonPropertyName ("targets")]
	public List<string>? Targets { get; set; }

	[JsonPropertyName ("developmentRegion")]
	public string? DevelopmentRegion { get; set; }

	[JsonPropertyName ("knownRegions")]
	public List<string>? KnownRegions { get; set; }

	[JsonPropertyName ("compatibilityVersion")]
	public string? CompatibilityVersion { get; set; }

	[JsonPropertyName ("productRefGroup")]
	public string? ProductRefGroup { get; set; }

	[JsonPropertyName ("attributes")]
	public Dictionary<string, string>? Attributes { get; set; }

	[JsonPropertyName ("hasScannedForEncodings")]
	public string? HasScannedForEncodings { get; set; }

	[JsonPropertyName ("mainGroup")]
	public string? MainGroup { get; set; }

	[JsonPropertyName ("projectDirPath")]
	public string? ProjectDirPath { get; set; }

	[JsonPropertyName ("projectRoot")]
	public string? ProjectRoot { get; set; }
}

class XCConfigurationList : XcodeObject {

	[JsonPropertyName ("defaultConfigurationIsVisible")]
	public string? DefaultConfigurationIsVisible { get; set; }

	[JsonPropertyName ("defaultConfigurationName")]
	public string? DefaultConfigurationName { get; set; }

	[JsonPropertyName ("buildConfigurations")]
	public List<string>? BuildConfigurations { get; set; }
}

class PBXSourcesBuildPhase : XcodeObject {
	[JsonPropertyName ("buildActionMask")]
	public string BuildActionMask { get; set; } = int.MaxValue.ToString ();

	[JsonPropertyName ("files")]
	public List<string>? Files { get; set; }

	[JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
	public string RunOnlyForDeploymentPostprocessing { get; set; } = "0";
}

class PBXGroup : XcodeObject {
	[JsonPropertyName ("name")]
	public string? Name { get; set; }

	[JsonPropertyName ("children")]
	public List<string> Children { get; set; } = [];

	[JsonPropertyName ("sourceTree")]
	public string SourceTree { get; set; } = "<group>";
}

class PBXNativeTarget : XcodeObject {

	[JsonPropertyName ("buildConfigurationList")]
	public string? BuildConfigurationList { get; set; }

	[JsonPropertyName ("productReference")]
	public string? ProductReference { get; set; }

	[JsonPropertyName ("productType")]
	public string? ProductType { get; set; }

	[JsonPropertyName ("productName")]
	public string? ProductName { get; set; }

	[JsonPropertyName ("buildPhases")]
	public List<string>? BuildPhases { get; set; }

	[JsonPropertyName ("dependencies")]
	public List<object>? Dependencies { get; set; }

	[JsonPropertyName ("name")]
	public string? Name { get; set; }

	[JsonPropertyName ("buildRules")]
	public List<object>? BuildRules { get; set; }
}
