# Parse SDK for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/uoit0ona7m3x9bw6?svg=true)](https://ci.appveyor.com/project/ParseCommunity/parse-sdk-dotnet)
[![codecov](https://codecov.io/gh/parse-community/Parse-SDK-dotNET/branch/master/graph/badge.svg)](https://codecov.io/gh/parse-community/Parse-SDK-dotNET)
[![Nuget][nuget-svg]][nuget-link]
[![License][license-svg]][license-link]

## Getting Started
The latest stable release of the SDK is available on our [NuGet package][nuget-link].
To use the most up-to-date code, build this project and reference the generated NuGet package.

## Using the Code
Make sure you are using the project's root namespace:

```cs
using Parse;
```

Then, in your program's entry point, paste the following code, with the bolded parts replaced to reflect your application and Parse Server setup.

<pre lang="cs">
ParseClient.Initialize(new ParseClient.Configuration
{
    ApplicationId = "<b>App ID</b>",
    WindowsKey = "<b>DotNet Key</b>",
    Server = "<b>Full URL pointing to <i>Hosted Parse</i> Server</b>"
});
</pre>

## Building The Library
You can build the library from Visual Studio Code (with the proper extensions), Visual Studio 2017 Community and higher, or Visual Studio for Mac 7 and higher. You can also build the library using the command line:

### On Windows or Unix-based systems with Dotnet SDK installed:
```batch
dotnet build Parse.sln
```

Results can be found in `Parse/bin`

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
 [parse-download-link]: http://docs.parseplatform.org/
