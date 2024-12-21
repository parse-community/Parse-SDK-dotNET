# [4.0.0](https://github.com/parse-community/Parse-SDK-dotNET/compare/3.0.2...4.0.0) (2024-12-19)


### Features

* Upgrade target framework from NET Standard 2.0 to .NET 6.0 ([#393](https://github.com/parse-community/Parse-SDK-dotNET/issues/393)) ([1d4ab13](https://github.com/parse-community/Parse-SDK-dotNET/commit/1d4ab1339a8b49a6ac406c66bb697fe17c6726b5))


### BREAKING CHANGES

* This release requires .NET 6.0 or later and removes compatibility with NET Standard 2.0; Xamarin developers should migrate to .NET MAUI to use this version of the Parse SDK; Unity developers should use the previous SDK version until Unity supports .NET. ([1d4ab13](1d4ab13))

## [3.0.2](https://github.com/parse-community/Parse-SDK-dotNET/compare/3.0.1...3.0.2) (2024-05-24)


### Initial Release!

* Cannot access objects without user login ([#368](https://github.com/parse-community/Parse-SDK-dotNET/issues/368)) ([aa278df](https://github.com/parse-community/Parse-SDK-dotNET/commit/aa278df8147516a2ff8a95e1fa0f5f7972c63cc4))

## [3.0.1](https://github.com/parse-community/Parse-SDK-dotNET/compare/3.0.0...3.0.1) (2024-05-24)


### Bug Fixes

* SDK crash on conversion of double type range values to long type ([#342](https://github.com/parse-community/Parse-SDK-dotNET/issues/342)) ([816ba02](https://github.com/parse-community/Parse-SDK-dotNET/commit/816ba02fa3765e01825da741cedb377eb53c97f6))

# [3.0.0](https://github.com/parse-community/Parse-SDK-dotNET/compare/2.0.0...3.0.0) (2024-05-23)


### Features

* Change license to Apache 2.0 ([#374](https://github.com/parse-community/Parse-SDK-dotNET/issues/374)) ([6887aff](https://github.com/parse-community/Parse-SDK-dotNET/commit/6887affb8f30683d47fdfaf00ccf8207576d3477))


### BREAKING CHANGES

* This changes the license to Apache 2.0. This release may contain breaking changes which are not listed here, so please make sure to test your app carefully when upgrading. ([6887aff](6887aff))