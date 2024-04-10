// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Projects;

#pragma warning disable CS9113 // Parameter is unread.

// Necessary plumbing code to hook up NSObject to its respective .h and .m files
public partial class GenObjcH (NSObject nsObject) : ITextTemplate;
public partial class GenObjcM (NSObject nsObject) : ITextTemplate;

#pragma warning restore CS9113 // Parameter is unread.
