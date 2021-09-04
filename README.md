# C# Promises

A lightweight Promises library derived from [RSG C# Promises](https://github.com/Real-Serious-Games/C-Sharp-Promise).

As of version 1.0.0 of this package, you are now able to install it, along with other Vulpes Software packages via the Unity Package Manager. 

In Unity 2019 LTS and Unity 2020 onwards you can install the package through 'Project Settings/Package Manager'. Under 'Scoped Registries' click the little '+' button and input the following into the fields on the right.

*Name:* Vulpes Software
*URL:* https://registry.npmjs.org
*Scope(s):* com.vulpes

Click 'Apply', now you should be able to access the Vulpes Software registry under the 'My Registries' section in the Package Manager window using the second dropdown in the top left.

## Using this library

To use this library efficiently you should first learn about Promises:

- [Promises on Wikpedia](http://en.wikipedia.org/wiki/Futures_and_promises)
- [Good overview](https://www.promisejs.org/)
- [Mozilla](https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise)

## Current Development Roadmap

Several features and changes are currently under investigation and may or may not be added to the package in the future.

*In Progress:*
- Restoration of missing functionality:
  - ThenAll
  - ThenRace
  - Finally
  - ContinueWith
  - Progress