# RecordsPlus

[![NuGet Status](https://img.shields.io/nuget/v/RecordsPlus.svg)](https://www.nuget.org/packages/RecordsPlus/)
[![Nuget Downloads](https://img.shields.io/nuget/dt/RecordsPlus.svg)](https://www.nuget.org/packages/RecordsPlus/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/distantcam/RecordsPlus/build.yml)](https://github.com/distantcam/RecordsPlus/actions/workflows/build.yml)
![GitHub last commit](https://img.shields.io/github/last-commit/distantcam/recordsplus)

RecordsPlus is a Roslyn Source Generator that provides the same features records have to classes and structs.

<a id='toc'></a>
toc

## NuGet packages

https://nuget.org/packages/RecordsPlus/

## Examples

Make the type `partial`, add `[Record]`, and delete the hand written equality and `ToString` members.

snippet: Person

<details><summary>What gets generated</summary>

snippet: Person.cs#Person.g.verified.cs

</details>

<a href='#toc' title='Back to Contents'>Back to Contents</a>
## Other

### Keeping Attributes In Code

The `[Record]` attributes are decorated with the `[Conditional]` attribute, so their usage will not appear in the build output of your project. If you use reflection at runtime you will not find the `[Record]` attributes.

If you wish to preserve these attributes in the build output, add the define constant `RECORDSPLUS_USAGES`.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!--  Define the MSBuild constant    -->
    <DefineConstants>$(DefineConstants);RECORDSPLUS_USAGES</DefineConstants>
  </PropertyGroup>

</Project>
```

<a href='#toc' title='Back to Contents'>Back to Contents</a>
