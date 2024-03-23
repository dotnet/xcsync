// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Projects;

// Necessary plumbing code to hook up NSObject to its respective .h and .m files
public partial class GenObjcH : ITextTemplate {
    public GenObjcH (NSObject nsObject)
    {
        
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        // Concrete types are not used here because the template is generated code and the
        // generated code isn't available until after the first build.
		var thisTemplate = this as ITextTemplate;
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		thisTemplate.Session = new Dictionary<string, object> { { "nsObject", nsObject } };
        thisTemplate.Initialize ();
    }
}


public partial class GenObjcM : ITextTemplate {
    public GenObjcM (NSObject nsObject)
    {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
        // Concrete types are not used here because the template is generated code and the
        // generated code isn't available until after the first build.
		var thisTemplate = this as ITextTemplate;
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
		thisTemplate.Session = new Dictionary<string, object> { { "nsObject", nsObject } };
        thisTemplate.Initialize ();
    }
}
