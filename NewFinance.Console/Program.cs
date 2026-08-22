using NewFinance.Concrete.Entities;
using NewFinance.Configuration;

Console.WriteLine("At any point, press Ctrl+C or close the command window to quit.");

Configuration? config = null;
while(config is null)
{
    switch (ReadOptions("I would like to ...", ('a', "Create a new config."), ('b', "Open an existing config.")))
    {
        case 0: // New
            {
                var fileLocation = Answer("File location:");
                if (File.Exists(fileLocation))
                {
                    bool overwrite = ReadYesOrNoUntilAnswered("File already exists. Overwrite");                    
                    if (!overwrite)
                    {
                        fileLocation = null;
                    }
                }
                else if (Directory.Exists(fileLocation))
                {
                    var fileName = Answer("File name:");
                    fileLocation = Path.Combine(fileLocation, fileName!);
                    if (Path.GetExtension(fileLocation) == "")
                    {
                        fileLocation += ".json";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(fileLocation))
                {
                    var cwd = Directory.GetCurrentDirectory();
                    fileLocation = Path.Combine(cwd, fileLocation);
                    var containingFolder = Path.GetDirectoryName(fileLocation);
                    if (!Directory.Exists(containingFolder))
                    {
                        Console.Write($"Folder {containingFolder} not found");
                        fileLocation = null;
                    }
                }
                if (fileLocation is not null)
                {
                    config = new Configuration();
                    // TODO json serialisation
                }
                else
                {
                    continue;   
                }
                break;
            }
        case 1:
            {
                Console.WriteLine("File location:");
                var fileLocation = Console.ReadLine();
                if (Directory.Exists(fileLocation))
                {
                    // TODO json deserialisation config from fileLocation 
                }
                else
                {
                    Console.WriteLine("File is not found.");
                }
                break;                    
            }
    }

    while (true)
    {
        bool addIndividual = false;
        if (config!.TaxIndividuals.Count < 1)
        {
            Console.WriteLine("Add the first tax individual ...");
            addIndividual = true;
        }
        else
        {
            if (ReadYesOrNoUntilAnswered("Add another tax individual (Y or N)"))
            {
                addIndividual = true;
            }
        }
        if (!addIndividual)
        {
            break;
        }

        var individual = new TaxIndividual();
        while (true)
        {
            var name = Answer("Name of the individual:");
            if (string.IsNullOrWhiteSpace(name))
            {
                individual.Name = name!.Trim();
                break;
            }
        }

        config.TaxIndividuals.Add(individual);
    }

    while (true)
    {
        
    }
}

static string? Answer(string question)
{
    Console.WriteLine(question);
    var answer = Console.ReadLine();
    return answer;
}

static int? ReadOptions(string question, params (char, string)[] options)
{
    Console.WriteLine($"{question}?");
    foreach (var (c,s) in options)
    {
        Console.WriteLine($"{char.ToUpper(c)}) {s}");
    }
    var key = Console.ReadKey(false);
    Console.WriteLine();

    int i = 0;
    foreach (var (c, _) in options)
    {
        if (char.ToUpper(c) == char.ToUpper(key.KeyChar))
        {
            return i;
        }
        i++;
    }

    return null;
}

static bool? ReadYesOrNo(string question)
{
    Console.WriteLine($"{question}? (Press Y or N)");
    var key = Console.ReadKey(false);
    Console.WriteLine();
    if (key.Key == ConsoleKey.Y)
    {
        return true;
    }
    if (key.Key == ConsoleKey.N)
    {
        return false;
    }
    return null;
}

static bool ReadYesOrNoUntilAnswered(string question)
{
    bool? answer;
    do
    {
    } while ((answer = ReadYesOrNo("File already exists. Overwrite")) == null);
    return answer!.Value;
}