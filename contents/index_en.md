---
title: MarkTheRipper document
lang: en-us
date: 10/26/2022 22:24:52 +09:00
---

![MarkTheRipper](Images/MarkTheRipper.100.png)

MarkTheRipper - Fantastic faster generates static site comes from simply Markdowns.

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

| Package  | NuGet                                                                                                                |
|:---------|:---------------------------------------------------------------------------------------------------------------------|
| MarkTheRipper | [![NuGet MarkTheRipper](https://img.shields.io/nuget/v/MarkTheRipper.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper) |
| MarkTheRipper.Core | [![NuGet MarkTheRipper.Core](https://img.shields.io/nuget/v/MarkTheRipper.Core.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper.Core) |
| MarkTheRipper.Engine | [![NuGet MarkTheRipper.Engine](https://img.shields.io/nuget/v/MarkTheRipper.Engine.svg?style=flat)](https://www.nuget.org/packages/MarkTheRipper.Engine) |

## CI

| main                                                                                                                                                                 | develop                                                                                                                                                                       |
|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [![MarkTheRipper CI build (main)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Amain) | [![MarkTheRipper CI build (develop)](https://github.com/kekyo/MarkTheRipper/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/MarkTheRipper/actions?query=branch%3Adevelop) |

----

[![Japanese language](Images/Japanese.256.png)](https://github.com/kekyo/MarkTheRipper/blob/main/README_ja.md)

## What is this?

TODO: Eventually this document will be converted by MarkTheRipper itself.

MarkTheRipper is a very simple and fast static site generator
that allows you to write content in markdown.
The main intended use is for blog sites,
but we have eliminated the need for complex structures and tools anyway,
as if you were writing an article on GitHub Gist likely.

If you already have .NET 6.0 installation, you can install it simply:

```bash
dotnet tool install -g MarkTheRipper
```

Alternatively, [you can download a binary distribution](https://github.com/kekyo/MarkTheRipper/releases) that is compatible with .NET Framework 4.71 or higher.

* Important as of 0.4.0, installation with dotnet tooling has a problem where the incorrect version is installed [(being fixed.)](https://github.com/kekyo/MarkTheRipper/issues/27)

----

## Install develop branch package

```
$ dotnet tool install -g MarkTheRipper --nuget-source http://nuget.kekyo.online:59103/repository/nuget/index.json
```

----

## License

Apache-v2.

----

## History

* 0.4.0:
  * Can be paging navigation. #20
* 0.3.0:
  * Ready to use oEmbed. #18
* 0.2.0:
  * Keyword expansion can be applied to the markdown document itself. #3
* 0.1.0:
  * Automatically inserted date metadata into markdown file when lacked. #13
  * Function calls can be made. #2
  * Aggregated categories and tags from entire content markdowns. #14
  * Bracket characters are supported. #6
  * Can use the generator keyword. #5
  * Automatic category collection is possible. #1
