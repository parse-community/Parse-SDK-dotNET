# Parse SDK for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/uoit0ona7m3x9bw6?svg=true)](https://ci.appveyor.com/project/ParseCommunity/parse-sdk-dotnet)
[![codecov](https://codecov.io/gh/parse-community/Parse-SDK-dotNET/branch/master/graph/badge.svg)](https://codecov.io/gh/parse-community/Parse-SDK-dotNET)
[![Nuget][nuget-svg]][nuget-link]
[![License][license-svg]][license-link]

## Getting Started
The SDK is available for download [on our website][parse-download-link] or our [NuGet package][nuget-link].

## Using hosted Parse
Before executing the following code, be sure to read the guide for Unity on [http://docs.parseplatform.org/unity/guide/](http://docs.parseplatform.org/unity/guide/)

```cs
using Parse;

// some code of initialization..

ParseClient.Initialize(new ParseClient.Configuration {
    ApplicationId = applicationID,
    WindowsKey = dotnetKey,

    // the serverURL of your hosted Parse Server
    Server = "<YOUR SERVER URL>"
});
```

## Building The Library
You can build the library from Visual Studio 2013+ or Xamarin IDE. You can also build the library using the command line:

```batch
:: In Windows:
MSBuild Parse.sln
```

```bash
# In Unix with Xamarin SDK installed:
xbuild Parse.sln
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
