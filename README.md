# Phf.Net

[![NuGet](https://img.shields.io/nuget/v/Phf.Net.svg)](https://www.nuget.org/packages/Phf.Net/)
[![Build Status](https://travis-ci.org/odinmillion/Phf.Net.svg?branch=master)](https://travis-ci.org/odinmillion/Phf.Net)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

Tiny perfect hash library for dotnet.
This is an implementation of the [CHD algorithm](http://cmph.sourceforge.net/chd.html) 

## Installation

This project is available as a [NuGet package](https://www.nuget.org/packages/Phf.Net/)

## Example

### Some code
```csharp
var strings = Enumerable.Range(0, 6).Select(x => $"str{x}").ToArray();
var settings = new PhfSettings {ItemsPerBucket = 4, Alpha = 80, Seed = 31337, NoDivision = true};
var hashFunction = PerfectHashFunction.Create(strings, settings);
foreach (var str in strings)
    Console.WriteLine($"{str} - {hashFunction.Evaluate(str)}");
```

### Output
```
str0 - 0
str1 - 4
str2 - 6
str3 - 3
str4 - 2
str5 - 5
```

## Hints

 * You can serialize created `PerfectHashFunction` for the future usage
