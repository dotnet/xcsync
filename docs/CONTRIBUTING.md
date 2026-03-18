# Contributing

This project welcomes contributions and suggestions. Most contributions require you to
agree to a Contributor License Agreement (CLA) declaring that you have the right to,
and actually do, grant us the rights to use your contribution. For details, visit
https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need
to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the
instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

# Set up your machine to contribute

These instructions will get you ready to contribute to this project. If you just want to use xcsync, see [using-latest-daily.md](using-latest-daily.md).

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
