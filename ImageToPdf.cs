#:property PackageLicenseExpression=MIT
#:package QuestPDF@*
#:package SkiaSharp@*
#:package System.CommandLine@*

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.RegularExpressions;
using ImageToPdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

// QuestPDF.Settings.License = LicenseType.Community;
var fileArgument = new Argument<FileInfo>("File")
{
    Description = "Path to the file.",
    Arity = ArgumentArity.ExactlyOne
};
fileArgument.AcceptLegalFilePathsOnly();
fileArgument.Validators.Add(result =>
{
    var file = result.GetRequiredValue(fileArgument);
    if (!file.Exists)
    {
        result.AddError($"The file '{file.Name}' was not found.");
    }
});

var outputOption = new Option<FileInfo>("--output", ["-o"])
{
    DefaultValueFactory = _ => new FileInfo("generated.pdf"),
    Arity = ArgumentArity.ExactlyOne,
    Description = "Path to save generated file"
};
outputOption.AcceptLegalFilePathsOnly();

var pageSizeOption = new Option<PageSize>("--pagesize", ["-p"])
{
    CustomParser = ParsePageSize,
    Arity = ArgumentArity.ExactlyOne,
    Required = true,
    Description = "Page size. Can be a standard name like 'a4' (see available below) or custom width and height (e.g. 20cmx30cm, see available units below)"
};

var marginOption = new Option<Rectangle>("--margin", ["-m"])
{
    CustomParser = ParseMargin,
    Arity = ArgumentArity.ExactlyOne,
    Required = true,
    Description = "Left and top margin sizes, to position the image (e.g. '5mmx7mm', see available units below)"
};
var widthOption = new Option<Length?>("--width", ["-w"])
{
    CustomParser = ParseDimension,
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Image width. This is one of the ways of indicating the physical width of the image (the other is --resolution)"
};
var resolutionOption = new Option<int?>("--resolution", ["-r"])
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "Resolution in dots per inch. This is one of the ways of indicating the image width in the page (the other is --width). It assumes that each pixel is a printer point and uses the printer resolution to deduce the physical width."
};

var watermarkOption = new Option<string?>("--watermark")
{
    Arity = ArgumentArity.ZeroOrOne,
    Required = false,
    Description = "Watermark text (optional)"
};

var headerTextOption = new Option<string?>(
    "--header")
{
    Description = "Page header text (optional)",
    Arity = ArgumentArity.ZeroOrOne,
    Required = false,
};


var rootCommand = new RootCommand("Convert images to single PDF file");
rootCommand.Arguments.Add(fileArgument);
rootCommand.Options.Add(outputOption);
rootCommand.Options.Add(marginOption);
rootCommand.Options.Add(widthOption);
rootCommand.Options.Add(resolutionOption);
rootCommand.Options.Add(watermarkOption);
rootCommand.Options.Add(headerTextOption);
rootCommand.Options.Add(pageSizeOption);
rootCommand.SetAction(Handle);

var defaultHelpOption = rootCommand.Options.OfType<HelpOption>().FirstOrDefault();
if (defaultHelpOption?.Action is HelpAction dha)
    defaultHelpOption.Action = new CustomHelpAction(dha);

await rootCommand.Parse(args).InvokeAsync();

return 0;

void Handle(ParseResult parseResult)
{
    var inputFile = parseResult.GetRequiredValue(fileArgument);
    var outputFile = parseResult.GetRequiredValue(outputOption);
    var watermark = parseResult.GetValue(watermarkOption);
    var headerText = parseResult.GetValue(headerTextOption);
    var marginSize = parseResult.GetRequiredValue(marginOption);
    var resolution = parseResult.GetValue(resolutionOption);
    var width = parseResult.GetValue(widthOption);
    var pageSize = parseResult.GetRequiredValue(pageSizeOption);

    Length finalWidth = (resolution, width) switch
    {
        (null, null) => throw new Exception("Need one of --resolution of --width"),
        (_, null) => new Length(SKBitmap.DecodeBounds(inputFile.FullName).Width / (float)resolution, Unit.Inch),
        (null, _) => width,
        _ => throw new Exception("Need only one of --resolution of --width")
    };
    Console.WriteLine("Final width: " + finalWidth);
    var doc = GenerateDocument(inputFile, watermark, headerText, marginSize, finalWidth, pageSize);
    doc.GeneratePdf(outputFile.FullName);
}

static Document GenerateDocument(FileInfo file, string? watermark, string? headerText, Rectangle marginSize, Length width, PageSize pageSize)
    => Document.Create(container =>
       container.Page(page =>
           FillPage(page, file.FullName, headerText, watermark, marginSize, width, pageSize)));

static void FillPage(PageDescriptor page, string filePath, string? headerText, string? watermark, Rectangle marginSize, Length width, PageSize pageSize)
{
    page.Size(pageSize);
    page.MarginTop(marginSize.Height, marginSize.Unit);
    page.MarginLeft(marginSize.Width, marginSize.Unit);

    FillHeader(page, headerText);

    page.Content().Layers(layers =>
    {
        layers.PrimaryLayer()
            .Width(width.Quantity, width.Unit)
            .Image(filePath);
        FillWatermark(layers, watermark);
    });
}

static void FillHeader(PageDescriptor page, string? headerText)
{
    if (string.IsNullOrEmpty(headerText))
    {
        return;
    }

    page.Header().AlignMiddle().Text(headerText);
}

static void FillWatermark(LayersDescriptor layers, string? watermark)
{
    if (string.IsNullOrEmpty(watermark))
    {
        return;
    }

    layers.Layer()
        .AlignCenter()
        .AlignMiddle()
        .Text(watermark)
        .FontSize(72).Bold().FontColor("#3afafafa");
}


static PageSize? ParsePageSize(ArgumentResult result)
{
    if (result.Tokens.Count == 0)
    {
        return null;
    }

    if (result.Tokens.Count > 2)
    {
        result.AddError("Need a page size such as a4 or 10cmx15cm");
        return null;
    }

    if (result.Tokens.Count == 1)
    {
        var ps = result.Tokens[0].Value;
        if (PageSizeConversion.ByName.TryGetValue(ps, out var pageSize)) return pageSize;
        result.AddError("Incorrect page size");
        return null;
    }

    return ParseRectangle(result)?.ToPageSize();
}

static Rectangle? ParseMargin(ArgumentResult result)
{
    if (result.Tokens.Count == 0)
    {
        return new Rectangle(5, 5, Unit.Millimetre);
    }

    return ParseRectangle(result);
}

static Rectangle? ParseRectangle(ArgumentResult result)
{
    if (result.Tokens.Count != 1)
    {
        result.AddError("Need a rectangle size such as 10cmx15cm");
        return null;
    }

    var split = result.Tokens[0].Value.Split("x");
    var (length1, message1) = ParseDimensionAux(split[0]);
    var (length2, message2) = ParseDimensionAux(split[1]);

    if (message1 != null || message2 != null)
    {
        result.AddError(string.Join(". ", message1, message2));
        return null;
    }

    if (length1!.Unit != length2!.Unit)
    {
        result.AddError("Units must be the same");
        return null;
    }

    return new Rectangle(length1.Quantity, length2.Quantity, length1.Unit);
}

static Length? ParseDimension(ArgumentResult result)
{
    if (result.Tokens.Count == 0)
    {
        return new Length(5, Unit.Point);
    }

    if (result.Tokens.Count != 1)
    {
        result.AddError("Need a dimension such as 15cm");
        return null;
    }

    var dim = result.Tokens[0].Value;
    var regex = Regexes.NumberPrefixRegex();
    var quantityMatch = regex.Match(dim);
    if (!quantityMatch.Success)
    {
        result.AddError("Dimension must have a quantity");
        return null;
    }

    var quantity = int.Parse(quantityMatch.Value);
    var unitMatch = dim[quantityMatch.Value.Length..];


    if (!UnitConversion.UnitMap2.TryGetValue(unitMatch, out var unit))
    {
        result.AddError("Invalid unit, must be one of " + string.Join(",", UnitConversion.UnitMap2.Keys));

        return null;
    }

    return new Length(quantity, unit);
}

static (Length?, string?) ParseDimensionAux(string dim)
{
    var regex = Regexes.NumberPrefixRegex();
    var quantityMatch = regex.Match(dim);
    if (!quantityMatch.Success)
    {
        return (null, "Dimension must have a quantity");
    }

    var quantity = int.Parse(quantityMatch.Value);
    var unitMatch = dim[quantityMatch.Value.Length..];


    if (!UnitConversion.UnitMap2.TryGetValue(unitMatch, out var unit))
    {
        return (null, "Invalid unit, must be one of " + string.Join(",", UnitConversion.UnitMap2.Keys));
    }

    return (new Length(quantity, unit), null);
}

namespace ImageToPdf
{
    static class UnitConversion
    {
        public static readonly Dictionary<Unit, string> UnitMap = new()
    {
        { Unit.Centimetre, "cm" },
        { Unit.Point, "pt" },
        { Unit.Millimetre, "mm" },
        { Unit.Meter, "m" },
        { Unit.Inch, "in" },
        { Unit.Mill, "mil" },
        { Unit.Feet, "ft" },
    };
        static UnitConversion()
        {
            UnitMap2 = [];
            foreach (var unit in Enum.GetValues<Unit>())
            {
                UnitMap2.Add(UnitMap[unit], unit);
            }
        }
        public static readonly Dictionary<string, Unit> UnitMap2;
    }

    static class PageSizeConversion
    {
        static PageSizeConversion()
        {

            ByName = [];
            var pageSizeProps =
                typeof(PageSizes)
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(prop => prop.PropertyType == typeof(PageSize));


            foreach (var pageSizeProp in pageSizeProps)
            {
                ByName.Add(pageSizeProp.Name.ToLowerInvariant(), (PageSize)pageSizeProp.GetValue(null)!);
            }
        }

        public static readonly Dictionary<string, PageSize> ByName;
    }
    record Length(float Quantity, Unit Unit)
    {
        public override string ToString()
        {

            return $"{Quantity}{UnitConversion.UnitMap[Unit]}";
        }
    }

    record Rectangle(float Width, float Height, Unit Unit)
    {
        public override string ToString()
        {

            return $"{Width}{UnitConversion.UnitMap[Unit]}x{Height}{UnitConversion.UnitMap[Unit]}";
        }

        public PageSize ToPageSize()
        {
            return new PageSize(Width, Height, Unit);
        }
    }

    partial class Regexes
    {
        [GeneratedRegex("^[0-9]+")]
        public static partial Regex NumberPrefixRegex();
    }
    internal class CustomHelpAction(HelpAction action) : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult)
        {
            int result = action.Invoke(parseResult);

            Console.WriteLine("Standard page sizes:");
            Console.WriteLine(string.Join(", ", PageSizeConversion.ByName.Keys));
            Console.WriteLine();
            Console.WriteLine("Measure units:");
            Console.WriteLine(string.Join(", ", UnitConversion.UnitMap2.Keys));
            Console.WriteLine();

            return result;

        }
    }
}
