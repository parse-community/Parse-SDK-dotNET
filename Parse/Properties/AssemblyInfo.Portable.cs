// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Runtime.CompilerServices;

// Platform specific libraries can see Parse internals.
[assembly: InternalsVisibleTo("Parse")]

// Internal visibility for platform-specific libraries.
[assembly: InternalsVisibleTo("Parse.WinRT")]
[assembly: InternalsVisibleTo("Parse.NetFx45")]
[assembly: InternalsVisibleTo("Parse.Phone")]

// Internal visibility for test libraries.
[assembly: InternalsVisibleTo("ParseTest.Integration.WinRT")]
[assembly: InternalsVisibleTo("ParseTest.Integration.NetFx45")]
[assembly: InternalsVisibleTo("ParseTest.Integration.Phone")]

[assembly: InternalsVisibleTo("ParseTest.Unit.NetFx45")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#if MONO
[assembly: InternalsVisibleTo("ParseTestIntegrationiOS")]
[assembly: InternalsVisibleTo("ParseTest.Integration.Android")]
#endif

#if UNITY
[assembly: InternalsVisibleTo("ParseTest.Integration.Unity")]
#endif