using System.Security.Cryptography.X509Certificates;
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
            Console.WriteLine($"Current {config.TaxIndividuals.Count} tax individuals:");
            foreach (var ind in config.TaxIndividuals)
            {
                Console.WriteLine($" {ind.Name}");
            }
            if (ReadYesOrNoUntilAnswered($"Add another or edit an existing tax individual"))
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
            if (!string.IsNullOrWhiteSpace(name))
            {
                individual = config.TaxIndividuals.FirstOrDefault(x=>x.Name == name);
                if (individual is null)
                {
                    individual = new TaxIndividual
                    {
                        Name = name!.Trim()
                    };
                    Console.WriteLine($"A new individual named {name} is added.");
                }
                else
                {
                    if (ReadYesOrNoUntilAnswered($"An existing individual named {name} is found. Rename it"))
                    {
                        name = Answer("Name of the individual to rename to (leave it blank to NOT rename):");
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            individual.Name = name!.Trim();
                        }
                    }
                }
                break;
            }
        }

        config.TaxIndividuals.Add(individual);
    }

    while (true)
    {
        bool addFamily = false;
        Console.WriteLine($"Current {config.Families.Count} families:");
        foreach (var fam in config.Families)
        {
            Console.WriteLine($" {fam.Name}");
        }
        if (ReadYesOrNoUntilAnswered($"Add or edit a family"))
        {
            addFamily = true;
        }
        if (!addFamily)
        {
            break;
        }

        Family? family = null;
        while (true)
        {
            var name = Answer("Name of the family:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                family = config.Families.FirstOrDefault(x=>x.Name == name);
                if (family is null)
                {
                    family = new Family
                    {
                        Name = name!.Trim()
                    };
                    Console.WriteLine($"A new family named {name} is added.");
                }
                else
                {
                    if (ReadYesOrNoUntilAnswered($"An existing family named {name} is found. Rename it"))
                    {
                        name = Answer("Name of the family to rename to (leave it blank to NOT rename):");
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            family.Name = name!.Trim();
                        }
                    }
                }
                break;
            }
        }
        while (true)
        {
            if (!ReadYesOrNoUntilAnswered($"Add or edit a family member"))
            {
                break;
            }

            Console.WriteLine($"Current {family.TaxMembers.Count} family members:");
            foreach (var ti in config.TaxIndividuals)
            {
                if (family.TaxMembers.Contains(ti))
                {
                    Console.WriteLine($" * {ti.Name}");
                }
                else
                {
                    Console.WriteLine($"   {ti.Name}");
                }
            }
            var name = Answer("Name of the member to add/remove:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var member = config.TaxIndividuals.FirstOrDefault(x=>x.Name == name);
                if(member is not null)
                {
                    if (family.TaxMembers.Contains(member))
                    {
                        family.TaxMembers.Remove(member);
                        Console.WriteLine("Member remobed.");
                    }
                    else
                    {
                        family.TaxMembers.Add(member);
                        Console.WriteLine("Member added.");
                    }
                }
            }
        }
        while (true)
        {
            var numDepsStr = Answer("Number of dependencies:");
            if (int.TryParse(numDepsStr, out var numDeps))
            {
                family.DependencyCount = numDeps;
                break;
            }
        }
    }
}

// End of the script.

static string? Answer(string question, string defaultValue = "")
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