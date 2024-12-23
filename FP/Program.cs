﻿//CP bundle --output D:\folder\bundleFile.txt

using System.CommandLine;
using System.Text;

var bundleOption = new Option<FileInfo>("--output", "File path and name");
var languageOption = new Option<string[]>("--language", "List of programming languages (or 'all' for all files)")
{
    IsRequired = true,
    ArgumentHelpName = "language"
};

var includeSourceOption = new Option<bool>("--include-source", "Include source code path and name as a comment in the bundle")
{
    IsRequired = false,
    ArgumentHelpName = "include-source"
};

var sortOption = new Option<string>("--sort", "Sort order for copying code files (alphabetical, type)")
{
    Arity = ArgumentArity.ZeroOrOne,
    ArgumentHelpName = "sort"
};

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from the source code before copying")
{
    IsRequired = false,
    ArgumentHelpName = "remove-empty-lines"
};

var authorOption = new Option<string>("--author", "Name of the file author")
{
    IsRequired = true,
    ArgumentHelpName = "author"
};

var bundleCommand = new Command("bundle", "Bundle code files to a single file");

bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(includeSourceOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);

bundleCommand.SetHandler((FileInfo output, string[] languages, bool includeSource, string sortOrder, bool removeEmptyLines) =>
{
    try
    {
        var filesToBundle = new List<string>();
        var currentDirectory = Directory.GetCurrentDirectory();

        string[] validExtensions;
        if (languages.Contains("all"))
        {
            validExtensions = new[] { "*.cs", "*.java", "*.py", "*.js", "*.cpp", "*.h" }; // Add more as needed
        }
        else
        {
            validExtensions = languages.Select(lang => GetFileExtension(lang)).ToArray();
        }

        foreach (var ext in validExtensions)
        {
            filesToBundle.AddRange(Directory.GetFiles(currentDirectory, ext, SearchOption.TopDirectoryOnly)
                .Where(file => !file.Contains("bin") && !file.Contains("debug")));
        }

        using (var fileStream = File.Create(output.FullName))
        using (var writer = new StreamWriter(fileStream))
        {
            if (includeSource)
            {
                writer.WriteLine($"// Source files included: {string.Join(", ", filesToBundle)}");
            }

            if (sortOrder == "alphabetical")
            {
                filesToBundle = filesToBundle.OrderBy(f => Path.GetFileName(f)).ToList();
            }
            else if (sortOrder == "type")
            {
                filesToBundle = filesToBundle.OrderBy(f => Path.GetExtension(f)).ToList();
            }

            foreach (var file in filesToBundle)
            {
                var fileContent = File.ReadAllLines(file);
                if (removeEmptyLines)
                {
                    fileContent = fileContent.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                writer.WriteLine($"// File: {Path.GetFileName(file)}");
                writer.WriteLine(string.Join(Environment.NewLine, fileContent));
            }

            Console.WriteLine("File was created successfully.");
        }
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: File path is invalid.");
    }
}, bundleOption, languageOption, includeSourceOption, sortOption, removeEmptyLinesOption);

var createRspCommand = new Command("create-rsp", "Create a response file with the specified options");

createRspCommand.AddOption(authorOption);
createRspCommand.AddOption(bundleOption);
createRspCommand.AddOption(languageOption);
createRspCommand.AddOption(includeSourceOption);
createRspCommand.AddOption(sortOption);
createRspCommand.AddOption(removeEmptyLinesOption);

createRspCommand.SetHandler((string author, FileInfo output, string[] languages, bool includeSource, string sortOrder, bool removeEmptyLines) =>
{
    var responseFileContent = new StringBuilder();

    string outputDirectory = PromptUser("Enter the output directory:");
    string languagesInput = PromptUser("Enter the value for --language (comma-separated, e.g., 'java,csharp'):");
    bool includeSourceInput = PromptUser("Should it include source path? (true/false):") == "true";
    string sortOrderInput = PromptUser("Enter the sort order (alphabetical/type):");
    bool removeEmptyLinesInput = PromptUser("Should it remove empty lines? (true/false):") == "true";

    if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(sortOrderInput))
    {
        Console.WriteLine("Invalid input. Please provide valid values.");
        return;
    }

    responseFileContent.AppendLine($"# Author: {author}");
    responseFileContent.AppendLine($"--output {outputDirectory}");
    responseFileContent.AppendLine($"--language {languagesInput}");
    responseFileContent.AppendLine($"--include-source {includeSourceInput}");
    responseFileContent.AppendLine($"--sort {sortOrderInput}");
    responseFileContent.AppendLine($"--remove-empty-lines {removeEmptyLinesInput}");

    string responseFilePath = $"{author.ToLower()}.rsp";
    File.WriteAllText(responseFilePath, responseFileContent.ToString());
    Console.WriteLine($"Response file '{responseFilePath}' created. You can run it using: dotnet @\"{responseFilePath}\"");
}, authorOption, bundleOption, languageOption, includeSourceOption, sortOption, removeEmptyLinesOption);

var rootCommand = new RootCommand("CLI Tool for bundling code files");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);

static string PromptUser(string question)
{
    Console.WriteLine(question);
    string? input = Console.ReadLine();

    if (string.IsNullOrEmpty(input))
    {
        throw new ArgumentException("Input cannot be empty.");
    }

    return input;
}

static string GetFileExtension(string language)
{
    return language.ToLower() switch
    {
        "csharp" => "*.cs",
        "java" => "*.java",
        "python" => "*.py",
        "javascript" => "*.js",
        "cpp" => "*.cpp",
        "c" => "*.c",
        "html" => "*.html",
        _ => throw new ArgumentException($"Unsupported language: {language}"),
    };
}

