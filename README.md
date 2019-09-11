# FreyaSessionProvider

![Nuget](https://img.shields.io/nuget/v/FreyaSessionProvider)

## About

This project is a session store for [Freya](https://freya.io). It utilizes Freya's optics to handle a session cookie, and has been written to support many different persistence products. It was originally written as part of the ["objects |> functions" project](https://objects-to-functions.bitbadger.solutions)" (step 4), but was extracted into its own package so as to not confuse the user with having to write their own session provider. I ran into several "gotcha"s, and I didn't want to scare off new-to-F# learners.

## Adding to Your Project

The RavenDB implementation is the only one so far; to use it, simply add the `FreyaSessionProvider.RavenDB` package via your package manager of choice.

## Using in Your Project

The `ISessionProvider` is the main type exposed by the project. The static function `SessionProvider.Create` will take a set of configuration options and create an instance that can be registered as a dependency of the application. There are 4 options that can be configured:

- `cookieName : string` - The name of the cookie, which defaults to `.FreyaSession`
- `expiry : TimeSpan` - For how long the session should be valid; this defaults to 1 hour
- `store : ISessionStore` - The session store implementation; this should be filled in with a constructed `ISessionStore` instance, which for the RavenDB provider is `RavenDBSessionStore`
- `crypto : SymmetricAlgorithm` - The algorithm to use for encrypting the cookie contents; defaults to null

`SessionProviderConfig.defaults` contains all the default values, but leaving the final two parameters as their default values will result in a session provider that does not work. Within this repository, there is [an F# script `GenerateCrypto.fsx`](blob/master/src/FreyaSessionProvider/GenerateCrypto.fsx) that will generate cryptography keys and output code that can be used to initialize the crypto setting. There's more on RavenDB below.

Once you have an `ISessionProvider` instance, there are four things you can do with it:

- `TryGetValue : (name : string) -> Freya<string option>` - This attempts to get a value from the session as a string. If the value exists, the string will be the `Some` value; if not, `None` is returned.
- `SetValue : (name : string) -> (value : string) -> Freya<unit>` - This sets a value in the session. If a session does not exist, it will create it. There is no arbitrary value on the size of the name or value; full objects could be passed as values using any serialization method that results in a string.
- `RemoveValue : (name : string) -> Freya<unit>` - This removes a value from the session; if there is no value stored under the specified name, it does nothing.
- `DestroySession : unit -> Freya<unit>` - This removes the session from the underlying store, and returns a cookie that will cause the session cookie to be expired.

The values are strings to provide maximum compatibility. One could easily compose functions around `TryGetValue` and `SetValue` to handle serialization and deserialization of whatever value needed to be stored.

## RavenDB Implementation

The RavenDB implementation just requires an initialized `IDocumentStore` instance. Assuming that instance is in the variable `docStore`, `RavenDBSessionStore docStore` would construct an instance of the store that can be used in the configuration options provided to `SessionProvider.Create`.

## Known Limitations

- This is the first release of this software; there are likely bugs. Feedback is welcome, and the "Issues" tab is open!
- Freya's current `SetCookie` header implementation only provides for setting one cookie per response. This provider assumes that header is its own, to do with as it pleases; if your project is returning another cookie, this will conflict with it.