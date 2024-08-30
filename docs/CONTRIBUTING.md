# Set up your machine to contribute

These instructions will get you ready to contribute to this project. If you just want to use xcsync, see [using-latest-daily.md](using-latest-daily.md).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](CODE_OF_CONDUCT.md). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Build the repo
`.\build.cmd` (Windows) or `./build.sh` (macOS and Linux)

## Localization

If you are contributing to xcsync, please ensure that all strings are localized. New strings are added to the `src/xcsync/Resources/Strings.resx` file.

## Generating local NuGet packages

If you want to try local changes it can be useful to generate the NuGet packages
in a local folder and use it as a package source.

To do so simply execute:
`.\build.cmd -pack` (Windows) or `./build.sh -pack` (macOS and Linux)

This will generate all the packages in the folder `./artifacts/packages/Debug/Shipping`. At this point from your solution folder run:

`dotnet nuget add source my_xcsync_folder/artifacts/packages/Debug/Shipping`

Or edit the `NuGet.config` file and add this line to the `<packageSources>` list:

```xml
<add key="xcsync-dev" value="my_xcsync_folder/artifacts/packages/Debug/Shipping" />
```