using System.CommandLine;

var input = new Option<FileInfo>(
    aliases: ["--input", "-i"],
    description: "The file with the words for permutation",
    isDefault: true,
    parseArgument: result =>
    {
        if (result.Tokens.Count == 0)
        {
            result.ErrorMessage = "Input file is required";
            return null!;
        }

        string path = result.Tokens.Single().Value;
        if (!File.Exists(path))
        {
            result.ErrorMessage = "Input file was not found";
            return null!;
        }

        return new FileInfo(path);
    });

var output = new Option<FileInfo>(
    aliases: ["--output", "-o"],
    description: "The file to write the dictionary of passwords",
    isDefault: true,
    parseArgument: result =>
    {
        if (result.Tokens.Count == 0)
        {
            result.ErrorMessage = "Output file is required";
            return null!;
        }

        string path = result.Tokens.Single().Value;
        return new FileInfo(path);
    });

var min = new Option<int>(
    name: "--min",
    description: "Minimum number of words",
    getDefaultValue: () => 1);

var max = new Option<int>(
    name: "--max",
    description: "Maximum number of words",
    getDefaultValue: () => int.MaxValue);

var leet = new Option<bool>(
    name: "--leet-mode",
    description: "Enable character replacements by similarity of their glyphs via reflection or other resemblance",
    getDefaultValue: () => false);

var root = new RootCommand("Program that creates a dictionary of possible passwords given a word set");
root.AddOption(input);
root.AddOption(output);
root.AddOption(leet);
root.AddOption(min);
root.AddOption(max);

root.SetHandler(Handle, input, output, leet, min, max);

return root.Invoke(args);

static void Handle(FileInfo input, FileInfo output, bool leet, int min, int max)
{
    var words = new List<string>();
    using (var read = input.OpenText())
    {
        string? line;
        while ((line = read.ReadLine()) is not null)
        {
            words.Add(line);
        }
    }

    if (leet)
        AddLeet(words);

    Console.WriteLine($"{words.Count} words found");

    long count = 0;
    using var write = output.AppendText();
    for (int i = 0; i < words.Count; i++)
    {
        if (i < min || i > max)
            continue;

        foreach (bool _ in WritePermutations(write, words, i + 1))
        {
            count++;
            if (count % 10_000_000 == 0)
                Console.WriteLine($"{count} passwords generated");
        }
    }

    Console.WriteLine($"{count} passwords created");
}

static IEnumerable<bool> WritePermutations(StreamWriter writer, List<string> set, int places)
{
    if (places == 0)
    {
        yield return true;
        writer.WriteLine();
        yield break;
    }

    for (int i = 0; i < set.Count; i++)
    {
        var subset = set.ToList();
        subset.RemoveAt(i);

        foreach (bool _ in WritePermutations(writer, subset, places - 1))
        {
            writer.Write(set[i]);
            yield return true;
        }
    }
}

static void AddLeet(List<string> words)
{
    Dictionary<char, char[]> leetDictionary = new()
    {
        { 'a', ['4', '@'] },
        { 'b', ['8'] },
        { 'c', ['[', '<', '('] },
        { 'd', [')', '>'] },
        { 'e', ['3', '&'] },
        { 'g', ['6', '9'] },
        { 'h', ['#'] },
        { 'i', ['1', '!', '|'] },
        { 'l', ['1'] },
        { 'o', ['0'] },
        { 'q', ['9'] },
        { 's', ['5'] },
        { 't', ['7', '+'] },
        { 'z', ['2', '%'] }
    };

    var newWords = new List<string>();
    foreach (string word in words)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (!leetDictionary.TryGetValue(char.ToLowerInvariant(word[i]), out var letters))
                continue;

            foreach (char newLetter in letters)
            {
                newWords.Add(
                    string.Create(
                        word.Length,
                        (word, i, newLetter),
                        static (span, tuple) =>
                        {
                            tuple.word.CopyTo(span);
                            span[tuple.i] = tuple.newLetter;
                        }));
            }
        }
    }

    words.AddRange(newWords);
}
