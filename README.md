# ImageToPdf
.Net global tool for convert images to PDF document

This tool uses an excellent library for generating pdf files called [QuestPDF](https://www.questpdf.com/).

## Installation
# 🖼->📄 img2pdf

To install this tool, please execute the following command on your PC:

```
dotnet tool install img2pdf --global
```

To update the tool, please use:

```
dotnet tool update img2pdf --global
```

And to remove:

```
dotnet tool uninstall img2pdf --global
```

## Usage

You can run:
```
img2pdf -h
```

to show help:

```
Description:
  Converts an image to a PDF file

Usage:
  img2pdf <File> [options]

Arguments:
  <File>  Path to the file.

Options:
  -o, --output <output>                 Path to save generated file [default: generated.pdf]
  -m, --margin <margin>                 Margin size [default: 5mmx5mm]
  -w, --width <width>                   Image width. This is one of the ways of indicating the physical width of the image (the other is --resolution)
  -r, --resolution <resolution>         Resolution in dots per inch. This is one of the ways of indicating the image width in the page. It assumes that each pixel is a printer point   
                                        and uses the printer resolution to deduce the physical width.
  --watermark <watermark>               Watermark text (optional)
  --header <header>                     Page header text (optional)
  -p, --pagesize <pagesize> (REQUIRED)  Page size ('a4' or '10cmx15cm')
  --version                             Show version information
  -?, -h, --help                        Show help and usage information
```

For example, we have an image: `1.png`
Executing the command:
```
algel.imageToPdf 1.png -p a4 -r 300
```
will create a `generated.pdf` file with an A4 page that contains the image at 300 pixels per inch.

Optionally a watermark can be set:
```
algel.imageToPdf 1.png -w "my watermark"
```