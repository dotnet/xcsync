#nullable disable

using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

public class XcodeProject {
    [JsonPropertyName ("classes")]
    public Dictionary<string, object> Classes { get; set; }

    [JsonPropertyName ("objectVersion")]
    public string ObjectVersion { get; set; }

    [JsonPropertyName ("archiveVersion")]
    public string ArchiveVersion { get; set; }

    [JsonConverter (typeof (XcodeProjectObjectsJsonConverter))]
    [JsonPropertyName ("objects")]
    public IDictionary<string, XcodeObject> Objects { get; set; }

    [JsonPropertyName ("rootObject")]
    public string RootObject { get; set; }
}

[JsonConverter (typeof (XcodeObjectConverter))]
public class XcodeObject {

    [JsonPropertyName ("isa")]
    [JsonPropertyOrder (1)]
    public string Isa { get; set; }

    string token;

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

public class PBXResourcesBuildPhase : XcodeObject {

    [JsonPropertyName ("buildActionMask")]
    [JsonPropertyOrder (2)]
    public string BuildActionMask { get; set; }

    [JsonPropertyName ("files")]
    [JsonPropertyOrder (3)]
    public List<string> Files { get; set; }

    [JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
    [JsonPropertyOrder (4)]
    public string RunOnlyForDeploymentPostprocessing { get; set; }
}

public class XCBuildConfiguration : XcodeObject {

    [JsonPropertyName ("buildSettings")]
    [JsonConverter (typeof (BuildSettingsConverter))]
    [JsonPropertyOrder (2)]
    public IDictionary<string, IList<string>> BuildSettings { get; set; }

    [JsonPropertyName ("name")]
    [JsonPropertyOrder (3)]
    public string Name { get; set; }
}

public class PBXFileReference : XcodeObject {

    [JsonPropertyName ("path")]
    [JsonPropertyOrder (2)]
    public string Path { get; set; }

    [JsonPropertyName ("includeInIndex")]
    [JsonPropertyOrder (3)]
    public string IncludeInIndex { get; set; }

    [JsonPropertyName ("name")]
    [JsonPropertyOrder (4)]
    public string Name { get; set; }

    [JsonPropertyName ("explicitFileType")]
    [JsonPropertyOrder (5)]
    public string ExplicitFileType { get; set; }

    [JsonPropertyName ("lastKnownFileType")]
    [JsonPropertyOrder (6)]
    public string LastKnownFileType { get; set; }

    [JsonPropertyName ("sourceTree")]
    [JsonPropertyOrder (7)]
    public string SourceTree { get; set; }
}

public class PBXBuildFile : XcodeObject {

    [JsonPropertyName ("fileRef")]
    [JsonPropertyOrder (2)]
    public string FileRef { get; set; }
}

public class PBXFrameworksBuildPhase : XcodeObject {

    [JsonPropertyName ("buildActionMask")]
    [JsonPropertyOrder (2)]
    public string BuildActionMask { get; set; }

    [JsonPropertyName ("files")]
    [JsonPropertyOrder (3)]
    public List<string> Files { get; set; }

    [JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
    [JsonPropertyOrder (4)]
    public string RunOnlyForDeploymentPostprocessing { get; set; }
}

public class PBXProject : XcodeObject {

    [JsonPropertyName ("buildConfigurationList")]
    [JsonPropertyOrder (2)]
    public string BuildConfigurationList { get; set; }

    [JsonPropertyName ("targets")]
    [JsonPropertyOrder (3)]
    public List<string> Targets { get; set; }

    [JsonPropertyName ("developmentRegion")]
    [JsonPropertyOrder (4)]
    public string DevelopmentRegion { get; set; }

    [JsonPropertyName ("knownRegions")]
    [JsonPropertyOrder (5)]
    public List<string> KnownRegions { get; set; }

    [JsonPropertyName ("compatibilityVersion")]
    [JsonPropertyOrder (6)]
    public string CompatibilityVersion { get; set; }

    [JsonPropertyName ("productRefGroup")]
    [JsonPropertyOrder (7)]
    public string ProductRefGroup { get; set; }

    [JsonPropertyName ("attributes")]
    [JsonPropertyOrder (8)]
    public Dictionary<string, string> Attributes { get; set; }

    [JsonPropertyName ("hasScannedForEncodings")]
    [JsonPropertyOrder (9)]
    public string HasScannedForEncodings { get; set; }

    [JsonPropertyName ("mainGroup")]
    [JsonPropertyOrder (10)]
    public string MainGroup { get; set; }

    [JsonPropertyName ("projectDirPath")]
    [JsonPropertyOrder (11)]
    public string ProjectDirPath { get; set; }

    [JsonPropertyName ("projectRoot")]
    [JsonPropertyOrder (12)]
    public string ProjectRoot { get; set; }
}

public class XCConfigurationList : XcodeObject {

    [JsonPropertyName ("defaultConfigurationIsVisible")]
    [JsonPropertyOrder (2)]
    public string DefaultConfigurationIsVisible { get; set; }

    [JsonPropertyName ("defaultConfigurationName")]
    [JsonPropertyOrder (3)]
    public string DefaultConfigurationName { get; set; }

    [JsonPropertyName ("buildConfigurations")]
    [JsonPropertyOrder (4)]
    public List<string> BuildConfigurations { get; set; }
}

public class PBXSourcesBuildPhase : XcodeObject {

    [JsonPropertyName ("buildActionMask")]
    [JsonPropertyOrder (2)]
    public string BuildActionMask { get; set; }

    [JsonPropertyName ("files")]
    [JsonPropertyOrder (3)]
    public List<object> Files { get; set; }

    [JsonPropertyName ("runOnlyForDeploymentPostprocessing")]
    [JsonPropertyOrder (4)]
    public string RunOnlyForDeploymentPostprocessing { get; set; }
}

public class PBXGroup : XcodeObject {
    [JsonPropertyName ("name")]
    [JsonPropertyOrder (2)]
    public string Name { get; set; }

    [JsonPropertyName ("children")]
    [JsonPropertyOrder (3)]
    public List<string> Children { get; set; }

    [JsonPropertyName ("sourceTree")]
    [JsonPropertyOrder (4)]
    public string SourceTree { get; set; }
}

public class PBXNativeTarget : XcodeObject {

    [JsonPropertyName ("buildConfigurationList")]
    [JsonPropertyOrder (2)]
    public string BuildConfigurationList { get; set; }

    [JsonPropertyName ("productReference")]
    [JsonPropertyOrder (3)]
    public string ProductReference { get; set; }

    [JsonPropertyName ("productType")]
    [JsonPropertyOrder (4)]
    public string ProductType { get; set; }

    [JsonPropertyName ("productName")]
    [JsonPropertyOrder (5)]
    public string ProductName { get; set; }

    [JsonPropertyName ("buildPhases")]
    [JsonPropertyOrder (6)]
    public List<string> BuildPhases { get; set; }

    [JsonPropertyName ("dependencies")]
    [JsonPropertyOrder (7)]
    public List<object> Dependencies { get; set; }

    [JsonPropertyName ("name")]
    [JsonPropertyOrder (8)]
    public string Name { get; set; }

    [JsonPropertyName ("buildRules")]
    [JsonPropertyOrder (9)]
    public List<object> BuildRules { get; set; }
}
