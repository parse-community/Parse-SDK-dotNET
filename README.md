# Parse SDK for .NET
![][appveyor-link]
[![Coverage Status][coverall-svg]][coverall-link]
[![Nuget][nuget-svg]][nuget-link]
[![License][license-svg]][license-link]

## Getting Started
The SDK is available for download [on our website][parse-download-link] or our [NuGet package][nuget-link].

## Building The Library
You can build the library from Visual Studio 2013+ or Xamarin IDE. You can also build the library using the command line:

```batch
# In Windows:
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

 [appveyor-link]: https://ci.appveyor.com/api/projects/status/ixidci5xsd9bcb4x/branch/master?svg=true
 [contributing]: https://github.com/ParsePlatform/Parse-SDK-dotNET/blob/master/CONTRIBUTING.md
 [coverall-link]: https://coveralls.io/github/ParsePlatform/Parse-SDK-dotNET?branch=master
 [coverall-svg]: https://coveralls.io/repos/ParsePlatform/Parse-SDK-dotNET/badge.svg?branch=master&service=github&t=Hh9XwS
 [license-svg]: https://img.shields.io/badge/license-BSD-lightgrey.svg
 [license-link]: https://github.com/ParsePlatform/Parse-SDK-dotNET/blob/master/LICENSE
 [nuget-link]: http://nuget.org/packages/parse
 [nuget-svg]: https://img.shields.io/nuget/v/parse.svg
 [parse-download-link]: https://parse.com/docs/downloads
