# Parse SDK for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/uoit0ona7m3x9bw6?svg=true)](https://ci.appveyor.com/project/ParseCommunity/parse-sdk-dotnet)
[![codecov](https://codecov.io/gh/parse-community/Parse-SDK-dotNET/branch/master/graph/badge.svg)](https://codecov.io/gh/parse-community/Parse-SDK-dotNET)
[![Nuget][nuget-svg]][nuget-link]
[![License][license-svg]][license-link]
[![Join The Conversation](https://img.shields.io/discourse/https/community.parseplatform.org/topics.svg)](https://community.parseplatform.org/c/parse-server)
[![Backers on Open Collective](https://opencollective.com/parse-server/backers/badge.svg)](#backers)
[![Sponsors on Open Collective](https://opencollective.com/parse-server/sponsors/badge.svg)](#sponsors)
![Twitter Follow](https://img.shields.io/twitter/follow/ParsePlatform.svg?label=Follow%20us%20on%20Twitter&style=social)

## Getting Started
The latest stable release of the SDK is available as a [NuGet package][nuget-link]. Note that the latest package currently available on the official distribution channel is quite old.
To use the most up-to-date code, build this project and reference the generated NuGet package.

## Using the Code
Make sure you are using the project's root namespace:

```cs
using Parse;
```

Then, in your program's entry point, paste the following code, with the text reflecting your application and Parse Server setup emplaced between the quotation marks.

```cs
ParseClient.Initialize(new ParseClient.Configuration
{
    ApplicationID = "",
    Key = "",
    ServerURI = ""
});
```

`ApplicationID` is your app's `ApplicationId` field from your Parse Server.
`Key` is your app's `DotNetKey` field from your Parse Server.
`ServerURI` is the full URL to your web-hosted Parse Server.

If you would like to, you can also set the `MasterKey` property, which will allow the SDK to bypass any CLPs and object permissions that are set. This property should be compatible with read-only master keys as well.

There are also a few optional parameters you can choose to set if you prefer or are experiencing issues with the SDK; sometimes the operation that generates values for these properties automatically can fail unexpectedly, causing the SDK to not be able to initialize, so these properties are provided to give you the ability to bypass that operation by providing the details outright.

`StorageConfiguration` represents some metadata information usually collected reflectively about the project for the purpose of data caching.
`VersionInfo` represents some version information usually collected reflectively about the project for the purposes of data caching and metadata collection for installation object creation.

To find full usage instructions for the latest stable release, please visit the [Parse docs website][parse-docs-link]. Please note that the latest stable release is quite old and does not reflect the work being done at the moment.

## Building The Library
You can build the library from Visual Studio Code (with the proper extensions), Visual Studio 2017 Community and higher, or Visual Studio for Mac 7 and higher. You can also build the library using the command line:

### On Windows or any .NET Core compatible Unix-based system with the .NET Core SDK installed:
```batch
dotnet build Parse.sln
```

Results can be found in either `Parse/bin/Release/netstandard2.0/` or `Parse/bin/Debug/netstandard2.0/` relative to the root project directory, where `/` is the path separator character for your system.

## How Do I Contribute?
We want to make contributing to this project as easy and transparent as possible. Please refer to the [Contribution Guidelines][contributing].

## License

```
Copyright (c) 2015-present, Parse, LLC.
All rights reserved.

This source code is licensed under the BSD-style license found in the
LICENSE file in the root directory of this source tree. An additional grant 
of patent rights can be found in the PATENTS file in the same directory.
```

 [contributing]: https://github.com/parse-community/Parse-SDK-dotNET/blob/master/CONTRIBUTING.md
 [license-svg]: https://img.shields.io/badge/license-BSD-lightgrey.svg
 [license-link]: https://github.com/parse-community/Parse-SDK-dotNET/blob/master/LICENSE
 [nuget-link]: http://nuget.org/packages/parse
 [nuget-svg]: https://img.shields.io/nuget/v/parse.svg
 [parse-docs-link]: http://docs.parseplatform.org/
