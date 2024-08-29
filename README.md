# Web IFC .NET

A [C++/CLI wrapper](https://en.wikipedia.org/wiki/C%2B%2B/CLI) around the 
[C++ code](https://github.com/ThatOpen/engine_web-ifc/tree/main/src/cpp) 
powering the [Web IFC library](https://github.com/ThatOpen/engine_web-ifc)
originally written by [Tom van Diggelen](https://github.com/tomvandig) 
and being developed by [That Open Company](https://github.com/ThatOpen).

## What is C++/CLI

C++/CLI is a variant of C++ that makes it easier to write interoperability code 
that communicates between C# and C++. This is not unlike how the C++ code is 
compiled using WASM to be usable from JavaScript.  

## Why?

Out goal was to significantly improve the IFC import service current in the Speckle server. 
https://github.com/specklesystems/speckle-server/blob/main/packages/fileimport-service/ifc/parser_v2.js

We wanted to create a project that replicates the behavior of the existing
Web IFC library, but with a .NET API, and that can consume updates to the main
C++ library as they are made to the [Web IFC library](https://github.com/ThatOpen/engine_web-ifc).

This project makes it possible to consume the Web IFC library from .NET
and allows us to better identify issues and performance bottlenecks. 

We can now:
* Quickly create helpful high-level libraries and GUI applications using C# 
* Step into and debug the C++ code, even from a C# application
* Quickly create unit, performance, and regression tests using NUnit
* Improve key algorithms with code written in C# 

## Code and Repository Structure

This repository consumes the Engine Web IFC repository as a sub-module: 
https://github.com/ThatOpen/engine_web-ifc. 

The solution contains three main projects:

- WebIfcClrWrapper 
    - this is a C++/CLI project. 
    - Only one file (DotNetApi.cpp) contains *managed* code.
    - The rest of the C++ source files are referenced directly from the Web IFC library submodule
- WebIfcDotNet 
    - This is a C# project that contains additional data types and algorithms to facilitate working with the .NET API
    - The ModelGraph class provides a high-level interface to an IFC file as a graph of nodes and relationships
    - It was designed specifically to address the needs of the Speckle project 
- WebIfcDotNetTests
	- This is a C# project that contains unit tests for the WebIfcDotNet project
	- It uses NUnit as the testing framework

There are temporarily two references to projects, which are not included and are not required.

- Ara3D.Speckle.Data 
- Ara3D.Speckle.Wpf 

These projects were part of an entry to the Speckle hackathon 2024: https://github.com/ara3d/speckle-desktop

# Acknowledgements

This work is sponsored by [Speckle](https://speckle.systems). 
