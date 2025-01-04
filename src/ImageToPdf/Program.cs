using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var filesOption = new Option<IEnumerable<FileInfo>>(
    "--file",
    "Path to the file. Multiple files are allowed.")
{
    Arity = ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true,
    IsRequired = true
};
filesOption.AddAlias("-f");
filesOption.LegalFilePathsOnly();
filesOption.AddValidator(result =>
{
    var files = result.GetValueForOption(filesOption) ?? [];
    var notExistingFiles = files.Where(f => !f.Exists).ToArray();
    if (notExistingFiles.Length > 0)
    {
        result.ErrorMessage = string.Join(Environment.NewLine,
            notExistingFiles.Select(f => $"The file '{f.Name}' was not found."));
    }
});

var outputOption = new Option<FileInfo>(
    "--output",
    () => new FileInfo("generated.pdf"),
    "Path to save generated file")
{
    Arity = ArgumentArity.ExactlyOne
};

outputOption.AddAlias("-o");
outputOption.LegalFilePathsOnly();


var marginOption = new Option<Length?>(
    ["--margin", "-m"],
    ParseDimension,
    true,
    "Margin size")
{
    Arity = ArgumentArity.ZeroOrOne
};
var fitOption = new Option<ImageFit>(
    "--fit",
    () => ImageFit.FitArea,
    "Fit method")
{
    Arity = ArgumentArity.ExactlyOne
};

var watermarkOption = new Option<string>(
    "--watermark",
    "Watermark text (optional)")
{
    Arity = ArgumentArity.ZeroOrOne,
    IsRequired = false
};
watermarkOption.AddAlias("-w");

var headerTextOption = new Option<string>(
    "--header",
    "Page header text (optional)")
{
    Arity = ArgumentArity.ZeroOrOne,
    IsRequired = false
};


var rootCommand = new RootCommand("Convert images to single PDF file");
rootCommand.AddOption(filesOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(marginOption);
rootCommand.AddOption(fitOption);
rootCommand.AddOption(watermarkOption);
rootCommand.AddOption(headerTextOption);
rootCommand.SetHandler(Handle);

await rootCommand.InvokeAsync(args);

return;

void Handle(InvocationContext context)
{
    var inputFiles = context.ParseResult.GetValueForOption(filesOption)!;
    var outputFile = context.ParseResult.GetValueForOption(outputOption)!;
    var watermark = context.ParseResult.GetValueForOption(watermarkOption);
    var headerText = context.ParseResult.GetValueForOption(headerTextOption);
    var marginSize = context.ParseResult.GetValueForOption(marginOption);
    var imageFit = context.ParseResult.GetValueForOption(fitOption);
    var doc = GenerateDocument(inputFiles, watermark, headerText, marginSize, imageFit);

    doc.GeneratePdf(outputFile.FullName);
}

static Document GenerateDocument(IEnumerable<FileInfo> files, string? watermark, string? headerText, Length? marginSize, ImageFit imageFit)
{
    return Document.Create(container =>
    {
        foreach (var file in files)
        {
            container.Page(page => FillPage(page, file.FullName, headerText, watermark, marginSize, imageFit));
        }
    });
}

static void FillPage(PageDescriptor page, string filePath, string? headerText, string? watermark, Length? marginSize, ImageFit imageFit)
{
    page.Size(PageSizes.A4);
    if (marginSize == null)
    {
        page.Margin(5);
    }
    else
    {
        page.Margin(marginSize.Quantity, marginSize.Unit);
    }

    FillHeader(page, headerText);

    page.Content().Layers(layers =>
    {
        var imageDescriptor = layers.PrimaryLayer().Image(filePath);
        switch (imageFit)
        {
            case ImageFit.FitArea:
                imageDescriptor.FitArea();
                break;
            case ImageFit.OriginalImage:
                imageDescriptor.UseOriginalImage();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(imageFit), imageFit, null);
        }
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

static Length? ParseDimension(ArgumentResult result)
{
    if (result.Tokens.Count == 0)
    {
        return new Length(5, Unit.Point);
    }

    if (result.Tokens.Count != 1)
    {
        result.ErrorMessage = "Need a dimension such as 15cm";
        return null;
    }

    var dim = result.Tokens[0].Value;
    var regex = new Regex("^[0-9]+");
    var quantityMatch = regex.Match(dim);
    if (!quantityMatch.Success)
    {
        result.ErrorMessage = "Dimension must have a quantity";
        return null;
    }

    var quantity = int.Parse(quantityMatch.Value);
    var unitMatch = dim[quantityMatch.Value.Length..];


    if (!UnitMaps.UnitMap2.TryGetValue(unitMatch, out var unit))
    {
        result.ErrorMessage = "Invalid unit, must be one of " + string.Join(",", UnitMaps.UnitMap2.Keys);
        
        return null;
    }

    return new Length(quantity, unit);
}

static class UnitMaps
{
    public static readonly Dictionary<Unit, string> UnitMap = new ()
    {
        { Unit.Centimetre, "cm" },
        { Unit.Point, "pt" },
        { Unit.Millimetre, "mm" },
        { Unit.Meter, "m" },
        { Unit.Inch, "in" },
        { Unit.Mil, "mil" },
        { Unit.Feet, "ft" },
    };
    static UnitMaps()
    {
        UnitMap2 = new();
        foreach (var unit in Enum.GetValues<Unit>())
        {
            UnitMap2.Add(UnitMap[unit], unit);
        }
    }
    public static readonly Dictionary<string, Unit> UnitMap2;
    
}
record Length(int Quantity, Unit Unit)
{
    public override string ToString()
    {
        
        return $"{Quantity}{UnitMaps.UnitMap[Unit]}";
    }
}

enum ImageFit
{
    FitArea,
    OriginalImage
}