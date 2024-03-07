// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//

namespace test_project;

[Register ("ViewController")]
partial class ViewController {
	[Outlet]
	AppKit.NSTextField FileLabel { get; set; }
	
	[Action ("UploadButton:")]
	partial void UploadButton (Foundation.NSObject sender);
	
	void ReleaseDesignerOutlets ()
	{
		if (FileLabel != null) {
			FileLabel.Dispose ();
			FileLabel = null;
		}
	}
}
