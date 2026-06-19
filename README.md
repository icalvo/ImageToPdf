# ImageToPdf

[![Build](https://github.com/icalvo/ImageToPdf/actions/workflows/main.yml/badge.svg)](https://github.com/icalvo/ImageToPdf/actions/workflows/main.yml)
[![NuGet](https://img.shields.io/nuget/v/ImageToPdf.svg)](https://www.nuget.org/packages/ImageToPdf)

.Net global tool for convert images to PDF document

This tool uses an excellent library for generating pdf files called [QuestPDF](https://www.questpdf.com/).

## Installation

To install this tool, please execute the following command on your PC:

```
dotnet tool install ImageToPdf --global
```

To update the tool, please use:

```
dotnet tool update ImageToPdf --global
```

And to remove:

```
dotnet tool uninstall ImageToPdf --global
```

## Publishing

Releases are published automatically to [nuget.org](https://www.nuget.org/packages/ImageToPdf) when a new version is pushed to `main`. To publish a release, bump the `Version` property in `ImageToPdf.cs` to a value higher than the latest version on NuGet, then push to `main`. The GitHub Actions workflow compares the version in the repository with the latest published package and only deploys when the local version is newer.

To list currently published versions locally, run `Get-Versions.ps1`.

## Usage

You can run:

```
imgtopdf -h
```

to show help:

```
Description:
  Convert images to single PDF file

Usage:
  ImageToPdf <File> [options]

Arguments:
  <File>  Path to the file. If not provided, it will be a new temporary file path.

Options:
  -o, --output <output>                 Path to save generated file [default: C:\Users\Administrator\AppData\Local\Temp\zvntpj2y.0v2.pdf]
  -m, --margin <margin> (REQUIRED)      Left and top margin sizes, to position the image (e.g. '5mmx7mm', see available units below)
  -w, --width <width>                   Image width. This is one of the ways of indicating the physical width of the image (the other is --resolution)
  -r, --resolution <resolution>         Resolution in dots per inch. This is one of the ways of indicating the image width in the page (the other is --width). It assumes that each pixel is a printer point and uses the printer resolution to deduce the physical width.
  --watermark <watermark>               Watermark text
  --header <header>                     Page header text
  -p, --pagesize <pagesize> (REQUIRED)  Page size. Can be a standard name like 'a4' (see available below) or custom width and height (e.g. 20cmx30cm, see available units below)
  -b, --border <border>                 Image border (e.g. 2mm, see available units below)
  -?, -h, --help                        Show help and usage information
  --version                             Show version information

Standard page sizes:
a0, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, b0, b1, b2, b3, b4, b5, b6, b7, b8, b9, b10, env10, envc4, envdl, executive, legal, letter, arch_a, arch_b, arch_c, arch_d, arch_e, arch_e1, arch_e2, arch_e3

Measure units:
pt, m, cm, mm, ft, in, mil


```

For example, we have an image: `1.png`
Executing the command:

```
imagetopdf 1.png -p a5 -m 1cmx1cm -r 300 -o generated.pdf
```

will create a `generated.pdf` file with an A4 page that contains the image at 300 pixels per inch, position at 1cm of the page borders.

Optionally a watermark can be set:

```
imagetopdf 1.png -p a4 -m 1cmx1cm -r 300 -o generated.pdf
```

