xcsync is a tool that enables developers to leverage Xcode for managing Apple specific files with .NET projects. The tool generates a temporary Xcode project from a .NET project and synchronizes changes to the Xcode files back to the .NET project.

Supported file types include:

* Asset catalog
* Plist
* Storyboard
* Xib
The tool has two commands: generate and sync. Use generate to create an Xcode project from a .NET project and sync to bring changes in the Xcode project back to the .NET project.

## Getting Started
Install the tool package using:
> dotnet tool install --global dotnet-xcsync

To use the tool with a .NET project that targets iOS, macOS, MacCatalyst, or tvOS Target Framework Monikers (TFM), use the following commands:

* Generate and open an Xcode project for a .NET project that uses the project file in the current directory, which supports the net9.0-ios TFM:

  ```
  dotnet generate -tfm net9.0-ios
  ```

* Sync changes from a generated Xcode project in the default location (./obj/Xcode) back to a .NET project that supports the net9.0-ios TFM:

  ```
  dotnet sync --project path/to/maui.csproj -tfm net9.0-ios
  ```

## Additional Information
Read more about xcsync by visiting the [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/maui/macios/xcsync) site.

## Feedback
If you encounter a bug or issues with this package, you can [open an Github issue](https://github.com/dotnet/xcsync/issues/new/choose). For more details, see [getting support](https://github.com/dotnet/xcsync/blob/main/.github/SUPPORT.md).