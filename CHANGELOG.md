# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2023-01-21
###Changed
- Resolved issue where 'Any' and 'Race' could cause exceptions to be thrown.
- Separated 'IPendingPromise' into 'IResolvable' and 'IProgressable'.
- Updated minimum Unity version from 2021.2 to 2021.3.

## [2.0.0] - 2022-03-14
###Changed
- Reworked source to bring it more in line with the original feature complete version of the RSG Promises library.

###Known Issues
- This update removes Promise pooling, however this feature will be reimplemented in a future verison of this package.

## [1.1.0] - 2021-09-04
###Changed
- Flagged IPromise.PromiseState as Obsolete, IPromise.State should be used instead.
- Cleaned up code.
- Fixed file extension for License file.

### Added
- Added method and property summaries.

## [1.0.0] - 2020-07-20
This is the first release of *Vulpes Promises* as a Package.