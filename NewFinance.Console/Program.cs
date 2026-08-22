using System.Security.Principal;
using NewFinance;
using NewFinance.Common;
using NewFinance.Concrete;
using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Contracts;
using NewFinance.Concrete.Entities;
using NewFinance.Configuration;
using NewFinance.Core;

Console.WriteLine("At any point, press Ctrl+C or close the command window to quit.");

var tempConfigFile = Path.Combine(
    Path.GetTempPath(),
    $"NewFinance-tempfile-{Guid.NewGuid():N}.json");

string? targetConfigFilePath = null;
string? workingConfigFilePath = null;

Configuration? config = null;
while(config is null)
{
    switch (ReadOptionsUntilAnswered("I would like to ...", ('a', "Create a new config."), ('b', "Open an existing config.")))
    {
        case 0: // New
            config = new Configuration();
            // Working file that is updated on the fly.
            // A later script (or an explicit “Save” action) can copy this
            // to the real destination (fileLocation).
            workingConfigFilePath = tempConfigFile;
            break;
        case 1:
            {
                var fileLocation = Answer("File location:");
                if (File.Exists(fileLocation))
                {
                    targetConfigFilePath = fileLocation;
                    File.Copy(fileLocation, tempConfigFile, true);
                    string json;
                    {
                        using var sr = new StreamReader(tempConfigFile);
                        json = sr.ReadToEnd(); 
                    }
                    config = SerializationHelper.Deserialize(json);
                    if (config is null)
                    {
                        Console.WriteLine($"Error opening config file {fileLocation}");
                    }
                    workingConfigFilePath = tempConfigFile;
                }
                else
                {
                    Console.WriteLine("File is not found.");
                }
                break;
            }
    }

    if (config is null || workingConfigFilePath is null)
    {
        // Opening an existing config is not implemented yet, so there is nothing to populate.
        continue;
    }

    while (true)
    {
        bool addIndividual = false;
        if (config.TaxIndividuals.Count < 1)
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

        while (true)
        {
            var name = Answer("Name of the individual:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                var individual = config.TaxIndividuals.FirstOrDefault(x=>x.Name == name);
                if (individual is null)
                {
                    individual = new TaxIndividual
                    {
                        Name = name
                    };
                    // Only a newly created individual is added, otherwise an existing one would be duplicated.
                    config.TaxIndividuals.Add(individual);
                    Console.WriteLine($"A new individual named {name} is added.");
                }
                else
                {
                    if (ReadYesOrNoUntilAnswered($"An existing individual named {name} is found. Rename it"))
                    {
                        name = Answer("Name of the individual to rename to (leave it blank to NOT rename):");
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            individual.Name = name.Trim();
                        }
                    }
                }
                break;
            }
        }
    }

    config.SaveToFile(workingConfigFilePath);

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
                name = name.Trim();
                family = config.Families.FirstOrDefault(x=>x.Name == name);
                if (family is null)
                {
                    family = new Family
                    {
                        Name = name
                    };
                    // The family used to be dropped on the floor here so nothing ever reached config.Families.
                    config.Families.Add(family);
                    Console.WriteLine($"A new family named {name} is added.");
                }
                else
                {
                    if (ReadYesOrNoUntilAnswered($"An existing family named {name} is found. Rename it"))
                    {
                        name = Answer("Name of the family to rename to (leave it blank to NOT rename):");
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            family.Name = name.Trim();
                        }
                    }
                }
                break;
            }
        }
        while (true)
        {
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
            if (!ReadYesOrNoUntilAnswered($"Add or remove a family member"))
            {
                break;
            }

            var name = Answer("Name of the member to add/remove:");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var member = config.TaxIndividuals.FirstOrDefault(x=>x.Name == name.Trim());
                if(member is not null)
                {
                    if (family.TaxMembers.Contains(member))
                    {
                        family.TaxMembers.Remove(member);
                        member.Family = null;
                        Console.WriteLine("Member removed.");
                    }
                    else
                    {
                        // AddTaxMember also back-links the individual to the family, which the tax rules rely on.
                        family.AddTaxMember(member);
                        Console.WriteLine("Member added.");
                    }
                }
                else
                {
                    Console.WriteLine($"No tax individual named {name.Trim()} is found.");
                }
            }
        }
        while (true)
        {
            var numDepsStr = Answer("Number of dependencies:", family.DependencyCount.ToString());
            if (int.TryParse(numDepsStr, out var numDeps) && numDeps >= 0)
            {
                family.DependencyCount = numDeps;
                break;
            }
        }
    }

    config.SaveToFile(workingConfigFilePath);

    while (true)
    {
        Console.WriteLine($"Current {config.Accounts.Count} accounts:");
        foreach (var acc in config.Accounts)
        {
            Console.WriteLine($" {DescribeAccount(acc)}");
        }
        if (!ReadYesOrNoUntilAnswered("Add or remove an account"))
        {
            break;
        }

        var accountName = Answer("Name of the account (leave it blank to cancel):");
        if (string.IsNullOrWhiteSpace(accountName))
        {
            continue;
        }
        accountName = accountName.Trim();

        var existingAccount = config.Accounts.FirstOrDefault(x => x.Name == accountName);
        if (existingAccount is not null)
        {
            // Account.Name is read-only, so an existing account can only be removed and re-created.
            if (ReadYesOrNoUntilAnswered($"An existing account named {accountName} is found. Remove it"))
            {
                RemoveAccount(config, existingAccount);
                Console.WriteLine("Account removed.");
            }
            continue;
        }

        Account? account = ReadOptionsUntilAnswered("Type of the account",
            ('c', "Cash or everyday account."),
            ('f', "Fund (shares, bonds, term deposit ...)."),
            ('p', "Property."),
            ('l', "Loan.")) switch
        {
            0 => new Account(accountName, ReadDecimal("Current balance:", 0m)),
            1 => CreateFundAccount(config, accountName),
            2 => CreatePropertyAccount(config, accountName),
            3 => CreateLoanAccount(config, accountName),
            _ => null
        };

        if (account is null)
        {
            continue;
        }

        // The loan helpers derive their own account name, so the duplicate check has to be repeated here.
        if (config.Accounts.Any(x => x.Name == account.Name))
        {
            Console.WriteLine($"An account named {account.Name} already exists. The new one is discarded.");
            continue;
        }

        config.Accounts.Add(account);
        Console.WriteLine($"{DescribeAccount(account)} is added.");

        AssignOwnership(config, account);

        var accountContracts = GetAccountContracts(account).ToList();
        if (accountContracts.Count > 0)
        {
            var isOptional = ReadYesOrNoUntilAnswered($"Is '{account.Name}' an optional (what-if) arrangement rather than an existing one");
            var isEnabled = isOptional && ReadYesOrNoUntilAnswered("Enable it by default");
            foreach (var contract in accountContracts)
            {
                RegisterContract(config, contract, isOptional, isEnabled);
            }
        }
    }

    config.SaveToFile(workingConfigFilePath);

    while (true)
    {
        Console.WriteLine($"Current {config.ExistingContracts.Count} existing contracts:");
        foreach (var contract in config.ExistingContracts)
        {
            Console.WriteLine($" {contract.Name}");
        }
        if (!ReadYesOrNoUntilAnswered("Add or remove an existing contract"))
        {
            break;
        }

        if (ReadYesOrNoUntilAnswered("Remove one instead of adding"))
        {
            RemoveContract(config, config.ExistingContracts.ToList());
            continue;
        }

        var newContract = CreateContract(config);
        if (newContract is not null)
        {
            config.ExistingContracts.Add(newContract);
            Console.WriteLine($"Contract '{newContract.Name}' is added.");
        }
    }

    config.SaveToFile(workingConfigFilePath);

    while (true)
    {
        Console.WriteLine($"Current {config.OptionalContracts.Count} optional contracts:");
        foreach (var (contract, enabled) in config.OptionalContracts)
        {
            Console.WriteLine($" {contract.Name} ({(enabled ? "enabled" : "disabled")} by default)");
        }
        if (!ReadYesOrNoUntilAnswered("Add, remove or toggle an optional contract"))
        {
            break;
        }

        switch (ReadOptionsUntilAnswered("I would like to ...",
            ('a', "Add an optional contract."),
            ('t', "Toggle whether one is enabled by default."),
            ('r', "Remove one.")))
        {
            case 0:
                {
                    var newContract = CreateContract(config);
                    if (newContract is not null)
                    {
                        var enabled = ReadYesOrNoUntilAnswered("Enable it by default");
                        config.OptionalContracts.Add((newContract, enabled));
                        Console.WriteLine($"Optional contract '{newContract.Name}' is added.");
                    }
                    break;
                }
            case 1:
                {
                    var toggled = SelectContract(config.OptionalContracts.Select(x => x.Item1).ToList(), "Contract to toggle:");
                    if (toggled is not null)
                    {
                        var index = config.OptionalContracts.FindIndex(x => x.Item1 == toggled);
                        var enabled = !config.OptionalContracts[index].Item2;
                        config.OptionalContracts[index] = (toggled, enabled);
                        Console.WriteLine($"'{toggled.Name}' is now {(enabled ? "enabled" : "disabled")} by default.");
                    }
                    break;
                }
            case 2:
                {
                    RemoveContract(config, config.OptionalContracts.Select(x => x.Item1).ToList());
                    break;
                }
        }
    }

    config.SaveToFile(workingConfigFilePath);

    // The tax assessments read the trackers the contracts above populate, so they are set up last and
    // therefore also end up last in ExistingContracts, which is the order the executor relies on.
    foreach (var individual in config.TaxIndividuals)
    {
        if (individual.Tax is not null)
        {
            continue;
        }
        if (!ReadYesOrNoUntilAnswered($"Set up the yearly tax assessment for {individual.Name}"))
        {
            continue;
        }
        var taxAccount = SelectAccount(config, $"Account {individual.Name} pays tax from and receives refunds into:");
        if (taxAccount is null)
        {
            continue;
        }
        var tax = new IndividualTax(individual, taxAccount).CreateAllNaturalTrackerKeys();
        individual.Tax = tax;
        config.ExistingContracts.Add(tax);
        Console.WriteLine($"'{tax.Name}' is added.");
    }

    config.SaveToFile(workingConfigFilePath);

    PrintConfigurationSummary(config);

    while (targetConfigFilePath is null)
    {
        var fileLocation = Answer("File location:");
        if (string.IsNullOrWhiteSpace(fileLocation))
        {
            fileLocation = null;
        }
        else if (File.Exists(fileLocation))
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
        else
        {
            var cwd = Directory.GetCurrentDirectory();
            fileLocation = Path.Combine(cwd, fileLocation);
            var containingFolder = Path.GetDirectoryName(fileLocation);
            if (!Directory.Exists(containingFolder))
            {
                Console.WriteLine($"Folder {containingFolder} not found");
                fileLocation = null;
            }
        }
        if (fileLocation is not null)
        {
            targetConfigFilePath = fileLocation;
        }
    }

    File.Copy(workingConfigFilePath, targetConfigFilePath, true);
    Console.WriteLine($"Config successfully saved to {targetConfigFilePath}");
}

// End of the script.

static string Answer(string question, string defaultValue = "")
{
    Console.WriteLine(question);
    if (!string.IsNullOrEmpty(defaultValue))
    {
        Console.Write($"[{defaultValue}] ");
    }
    var answer = Console.ReadLine();
    // A blank answer accepts the offered default rather than entering an empty string.
    return string.IsNullOrWhiteSpace(answer) ? defaultValue : answer;
}

static string AnswerUntilAnswered(string question)
{
    while (true)
    {
        var answer = Answer(question);
        if (!string.IsNullOrWhiteSpace(answer))
        {
            return answer.Trim();
        }
    }
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

static int ReadOptionsUntilAnswered(string question, params (char, string)[] options)
{
    int? answer;
    do
    {
    } while ((answer = ReadOptions(question, options)) == null);
    return answer!.Value;
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
    } while ((answer = ReadYesOrNo(question)) == null);
    return answer!.Value;
}

static decimal ReadDecimal(string question, decimal defaultValue)
{
    while (true)
    {
        var text = Answer(question, FormatNumber(defaultValue));
        if (decimal.TryParse(text, out var value))
        {
            return value;
        }
        Console.WriteLine("Please enter a valid number.");
    }
}

static decimal? ReadOptionalDecimal(string question)
{
    while (true)
    {
        var text = Answer($"{question} (leave it blank for none):");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (decimal.TryParse(text, out var value))
        {
            return value;
        }
        Console.WriteLine("Please enter a valid number.");
    }
}

static int ReadPositiveInt(string question, int defaultValue)
{
    while (true)
    {
        var text = Answer(question, defaultValue.ToString());
        if (int.TryParse(text, out var value) && value > 0)
        {
            return value;
        }
        Console.WriteLine("Please enter a positive whole number.");
    }
}

/// <summary>
///  Reads a rate given either as a percentage (5%) or as a factor (0.05).
/// </summary>
static decimal ReadRate(string question, decimal defaultValue)
{
    while (true)
    {
        var text = Answer(question, FormatNumber(defaultValue));
        if (TryParseRate(text, out var value))
        {
            return value;
        }
        Console.WriteLine("Please enter a rate, e.g. 5% or 0.05.");
    }
}

static bool TryParseRate(string text, out decimal value)
{
    value = 0m;
    if (string.IsNullOrWhiteSpace(text))
    {
        return false;
    }
    text = text.Trim();
    var isPercentage = text.EndsWith('%');
    if (isPercentage)
    {
        text = text[..^1].TrimEnd();
    }
    if (!decimal.TryParse(text, out value))
    {
        return false;
    }
    if (isPercentage)
    {
        value /= 100m;
    }
    return true;
}

static DateTime ReadDate(string question, DateTime? defaultValue = null)
{
    while (true)
    {
        var text = Answer($"{question} (yyyy-MM-dd)", defaultValue?.ToString("yyyy-MM-dd") ?? "");
        if (DateTime.TryParse(text, out var value))
        {
            return value.Date;
        }
        Console.WriteLine("Please enter a valid date, e.g. 2026-06-30.");
    }
}

static DateTime? ReadOptionalDate(string question)
{
    while (true)
    {
        var text = Answer($"{question} (yyyy-MM-dd, leave it blank for none):");
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (DateTime.TryParse(text, out var value))
        {
            return value.Date;
        }
        Console.WriteLine("Please enter a valid date, e.g. 2026-06-30.");
    }
}

static string FormatNumber(decimal value) => value.ToString("0.######");

static string DescribeAccount(Account account)
{
    var kind = account switch
    {
        Property => "property",
        Fund => "fund",
        Loan => "loan",
        Investment => "investment",
        _ => "cash"
    };
    return $"{account.Name} ({kind}, balance {account.Balance:N2})";
}

/// <summary>
///  The contracts an account brings with it, which are the ones that have to be registered with the
///  configuration on the account's behalf.
/// </summary>
static IEnumerable<Contract> GetAccountContracts(Account account)
{
    if (account is Investment investment && investment.Schedule is not null)
    {
        yield return investment.Schedule;
    }
    if (account is Loan loan && loan.Contract is not null)
    {
        yield return loan.Contract;
    }
}

static void RegisterContract(Configuration config, Contract contract, bool isOptional, bool isEnabled)
{
    if (isOptional)
    {
        config.OptionalContracts.Add((contract, isEnabled));
    }
    else
    {
        config.ExistingContracts.Add(contract);
    }
}

static Account? SelectAccount(Configuration config, string question, Func<Account, bool>? filter = null)
{
    var candidates = config.Accounts.Where(x => filter is null || filter(x)).ToList();
    if (candidates.Count == 0)
    {
        Console.WriteLine("No suitable account is available yet. Add one first.");
        return null;
    }
    while (true)
    {
        Console.WriteLine(question);
        foreach (var account in candidates)
        {
            Console.WriteLine($" {DescribeAccount(account)}");
        }
        var name = Answer("Name of the account (leave it blank to cancel):");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var found = candidates.FirstOrDefault(x => x.Name == name.Trim());
        if (found is not null)
        {
            return found;
        }
        Console.WriteLine("Account is not found.");
    }
}

static TaxIndividual? SelectIndividual(Configuration config, string question)
{
    if (config.TaxIndividuals.Count == 0)
    {
        Console.WriteLine("No tax individual has been added yet.");
        return null;
    }
    while (true)
    {
        Console.WriteLine(question);
        foreach (var individual in config.TaxIndividuals)
        {
            Console.WriteLine($" {individual.Name}");
        }
        var name = Answer("Name of the individual (leave it blank to cancel):");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var found = config.TaxIndividuals.FirstOrDefault(x => x.Name == name.Trim());
        if (found is not null)
        {
            return found;
        }
        Console.WriteLine("Individual is not found.");
    }
}

static Entity? SelectEntity(Configuration config, string question)
{
    var candidates = config.Families.Cast<Entity>().Concat(config.TaxIndividuals).ToList();
    if (candidates.Count == 0)
    {
        Console.WriteLine("No family or tax individual has been added yet.");
        return null;
    }
    while (true)
    {
        Console.WriteLine(question);
        foreach (var entity in candidates)
        {
            Console.WriteLine($" {entity.Name} ({(entity is Family ? "family" : "individual")})");
        }
        var name = Answer("Name of the owner (leave it blank to cancel):");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var found = candidates.FirstOrDefault(x => x.Name == name.Trim());
        if (found is not null)
        {
            return found;
        }
        Console.WriteLine("Owner is not found.");
    }
}

static Contract? SelectContract(List<Contract> contracts, string question)
{
    if (contracts.Count == 0)
    {
        Console.WriteLine("No contract is available.");
        return null;
    }
    while (true)
    {
        Console.WriteLine(question);
        foreach (var contract in contracts)
        {
            Console.WriteLine($" {contract.Name}");
        }
        var name = Answer("Name of the contract (leave it blank to cancel):");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var found = contracts.FirstOrDefault(x => x.Name == name.Trim());
        if (found is not null)
        {
            return found;
        }
        Console.WriteLine("Contract is not found.");
    }
}

static void AssignOwnership(Configuration config, Account account)
{
    var isLiability = account is Loan;
    while (true)
    {
        Console.WriteLine($"Current {account.Ownership.Count} owners of '{account.Name}':");
        foreach (var (owner, share) in account.Ownership)
        {
            Console.WriteLine($" {owner.Name}: {share:P2}");
        }
        if (!ReadYesOrNoUntilAnswered($"Add or remove an owner of '{account.Name}'"))
        {
            break;
        }

        var entity = SelectEntity(config, "Owner:");
        if (entity is null)
        {
            continue;
        }

        var accountEntityIndex = account.GetEntityIndex(entity); 
        if (accountEntityIndex is not null)
        {
            if (ReadYesOrNoUntilAnswered($"{entity.Name} already owns '{account.Name}'. Remove the ownership"))
            {
                account.Ownership.RemoveAt(accountEntityIndex.Value);
                entity.Assets.Remove(account);
                entity.Liabilities.Remove(account);
                Console.WriteLine("Ownership removed.");
            }
            continue;
        }

        var ownershipShare = ReadRate("Ownership share (e.g. 50% or 0.5):", 1m);
        if (isLiability)
        {
            entity.AddLiability(account, ownershipShare);
        }
        else
        {
            entity.AddAsset(account, ownershipShare);
        }
    }
}

static void RemoveAccount(Configuration config, Account account)
{
    config.Accounts.Remove(account);
    foreach (var contract in GetAccountContracts(account))
    {
        config.ExistingContracts.Remove(contract);
        config.OptionalContracts.RemoveAll(x => x.Item1 == contract);
    }
    foreach (var owner in account.Ownership.Select(x=>x.Entity))
    {
        owner.Assets.Remove(account);
        owner.Liabilities.Remove(account);
    }
    account.Ownership.Clear();
}

static void RemoveContract(Configuration config, List<Contract> contracts)
{
    var contract = SelectContract(contracts, "Contract to remove:");
    if (contract is null)
    {
        return;
    }
    if (config.Accounts.Any(x => GetAccountContracts(x).Contains(contract)))
    {
        Console.WriteLine("This contract belongs to an account. Remove the account instead.");
        return;
    }
    config.ExistingContracts.Remove(contract);
    config.OptionalContracts.RemoveAll(x => x.Item1 == contract);
    foreach (var individual in config.TaxIndividuals)
    {
        individual.TaxableContracts.Remove(contract);
        if (ReferenceEquals(individual.Tax, contract))
        {
            individual.Tax = null;
        }
    }
    Console.WriteLine($"Contract '{contract.Name}' is removed.");
}

/// <summary>
///  Builds a flow that starts at a given yearly amount and is reviewed on every anniversary of its start.
///  ApplyInflation only reviews on the dates it is given, so the anniversaries have to be passed in
///  explicitly for the increase to ever take effect.
/// </summary>
static BandedFlowDescriptor ReadFlowDescriptor(string amountQuestion, bool isInflow)
{
    var startTime = ReadDate("Start date:");
    var yearlyAmount = Math.Abs(ReadDecimal(amountQuestion, 0m));
    var increaseRate = ReadRate("Annual increase rate (e.g. 3%):", 0m);

    var reviewDates = new List<DateTime>();
    if (increaseRate != 0m)
    {
        var years = ReadPositiveInt("Apply that increase yearly for how many years:", 50);
        for (var i = 1; i <= years; i++)
        {
            reviewDates.Add(startTime.AddYears(i));
        }
    }

    var inflation = FlowHelpers.ConstantInflation(startTime, increaseRate);
    var descriptor = inflation.ApplyInflation(startTime, yearlyAmount / Constants.DaysPerYear, reviewDates);

    var yearlyCap = ReadOptionalDecimal("Cap on the yearly amount");
    if (yearlyCap.HasValue)
    {
        FlowHelpers.FlowCapping(descriptor, Math.Abs(yearlyCap.Value) / Constants.DaysPerYear, false);
    }

    if (!isInflow)
    {
        NegateFlow(descriptor);
    }

    var endTime = ReadOptionalDate("End date");
    if (endTime.HasValue && endTime.Value > startTime)
    {
        TruncateFlow(descriptor, endTime.Value);
    }

    return descriptor;
}

static void NegateFlow(BandedFlowDescriptor descriptor)
{
    ScaleFlow(descriptor, -1m);
}

static void ScaleFlow(BandedFlowDescriptor descriptor, decimal factor)
{
    for (var i = 0; i < descriptor.Inflows.Count; i++)
    {
        var (dailyRate, endTime) = descriptor.Inflows[i];
        descriptor.Inflows[i] = (dailyRate * factor, endTime);
    }
}

/// <summary>
///  Cuts the flow short at the given time by ending the band that covers it and appending a zero band,
///  which is how a BandedFlow is stopped while staying loaded in the executor.
/// </summary>
static void TruncateFlow(BandedFlowDescriptor descriptor, DateTime endTime)
{
    for (var i = 0; i < descriptor.Inflows.Count; i++)
    {
        if (descriptor.Inflows[i].EndTime >= endTime)
        {
            descriptor.Inflows[i] = (descriptor.Inflows[i].DailyRate, endTime);
            descriptor.Inflows.RemoveRange(i + 1, descriptor.Inflows.Count - i - 1);
            break;
        }
    }
    descriptor.Inflows.Add((0m, DateTime.MaxValue));
}

static Fund? CreateFundAccount(Configuration config, string name)
{
    var cashAccount = SelectAccount(config, "Cash account the distributions are paid into and the fees are paid from:");
    if (cashAccount is null)
    {
        return null;
    }

    var startTime = ReadDate("Date the value below is quoted at:");
    var initialValue = ReadDecimal("Current value:", 0m);
    var growthRate = ReadRate("Annual capital growth rate (e.g. 6%):", 0m);
    var valueCap = ReadOptionalDecimal("Value at which the growth stops");
    var yieldRate = ReadRate("Annual distribution/yield rate (e.g. 4%):", 0m);
    var payoutRate = ReadRate("Portion of the distribution paid out as cash, the rest being reinvested:", 0m);
    var feeRate = ReadRate("Annual management fee as a rate of the value (e.g. 0.2%):", 0m);
    var feeCap = ReadOptionalDecimal("Cap on the yearly management fee");
    // FeePeriod drives ContractHelpers.RunPeriodic, which never terminates on a zero period.
    var feePeriodDays = ReadPositiveInt("The fee is charged every how many days:", 365);

    Func<decimal, (Account, decimal)>? cash = null;
    if (payoutRate > 0m)
    {
        cash = yield => (cashAccount, yield * payoutRate);
    }

    var fund = new Fund(name);
    var schedule = new FundSchedule(
        fund,
        startTime,
        initialValue,
        value => valueCap.HasValue && value >= valueCap.Value ? 0m : growthRate,
        (s, period) => s.Investment.Balance * yieldRate * (decimal)period.TotalDays / Constants.DaysPerYear,
        cash)
    {
        FeePeriod = TimeSpan.FromDays(feePeriodDays),
        FeeRateToValue = feeRate,
        FeeCap = feeCap,
        FeePaymentAccount = cashAccount
    }.CreateAllNaturalTrackerKeys();
    fund.Schedule = schedule;

    return fund;
}

static Property? CreatePropertyAccount(Configuration config, string name)
{
    var cashAccount = SelectAccount(config, "Cash account the rates and fees are paid from and the rent is paid into:");
    if (cashAccount is null)
    {
        return null;
    }

    var purchaseTime = ReadDate("Purchase (contract exchange) date:");
    var purchasePrice = ReadDecimal("Purchase price:", 0m);
    var purchaseAdditionalCost = ReadDecimal("Additional purchase cost (stamp duty, legals ...):", 0m);
    var initialTime = ReadDate("Date the value below is quoted at:", purchaseTime);
    var initialValue = ReadDecimal("Value at that date:", purchasePrice);
    var growthRate = ReadRate("Annual capital growth rate (e.g. 4%):", 0m);
    var valueCap = ReadOptionalDecimal("Value at which the growth stops") ?? decimal.MaxValue;
    var baseFeeRate = ReadDecimal("Yearly base costs (rates, insurance, strata ...):", 0m);
    var rentalFeeRate = ReadDecimal("Additional yearly costs while it is rented out:", 0m);
    var feeInflationRate = ReadRate("Annual inflation applied to those costs (e.g. 3%):", 0m);

    var property = PropertyHelpers.CreatePropertyWithSchedule(name, purchaseTime, purchasePrice, purchaseAdditionalCost,
        initialTime, initialValue, growthRate, valueCap, baseFeeRate, rentalFeeRate, feeInflationRate, cashAccount);

    property.IsPurchasedAsNewBuild = ReadYesOrNoUntilAnswered("Is it purchased as a new build");

    if (ReadYesOrNoUntilAnswered("Is it rented out (an investment property)"))
    {
        Console.WriteLine($"Rental income of {name} ...");
        var inducedRate = ReadRate("Portion of the rent taken by the agent and other rent-proportional costs:", 0m);
        var descriptor = ReadFlowDescriptor("Current yearly gross rent:", true);
        if (inducedRate != 0m)
        {
            ScaleFlow(descriptor, 1m - inducedRate);
        }
        property.Schedule!.RentInducedStream = new BandedFlow(descriptor, cashAccount, $"Rent for {name}").CreateAllNaturalTrackerKeys();
    }

    return property;
}

static Loan? CreateLoanAccount(Configuration config, string name)
{
    var cashAccount = SelectAccount(config, "Cash account the repayments are made from:");
    if (cashAccount is null)
    {
        return null;
    }

    Property? property = null;
    if (config.Accounts.OfType<Property>().Any() && ReadYesOrNoUntilAnswered("Is it secured against a property"))
    {
        property = SelectAccount(config, "Property:", x => x is Property) as Property;
    }

    var loanAmount = ReadDecimal("Amount still owing:", 0m);
    var annualInterestRate = ReadRate("Annual interest rate (e.g. 5.5%):", 0m);
    var loanTermYears = ReadOptionalDecimal("Loan term in years, which is interest only if it is not given");

    if (property is not null)
    {
        var deposit = ReadOptionalDecimal("Deposit still to be paid at the purchase date");
        var settlementTime = ReadDate("Settlement date:", property.Schedule!.PurchaseTime);
        var offsetRatio = ReadRate("Portion of the cash account balance offsetting the loan:", 0m);
        var loan = PropertyHelpers.CreatePropertyLoan(property, deposit, settlementTime, loanAmount, cashAccount, offsetRatio, loanTermYears, annualInterestRate);
        Console.WriteLine($"The loan is named '{loan.Name}' after the property it is secured against.");
        return loan;
    }
    else
    {
        var startTime = ReadDate("Loan start date:");
        var loan = PropertyHelpers.CreatePersonalLoan(name, startTime, loanAmount, cashAccount, loanTermYears, annualInterestRate);
        Console.WriteLine($"The loan is named '{loan.Name}'.");
        return loan;
    }
}

static Contract? CreateContract(Configuration config)
{
    return ReadOptionsUntilAnswered("Type of the contract",
        ('e', "Employment income of a tax individual."),
        ('d', "Deductible expense of a tax individual."),
        ('s', "Super contribution."),
        ('f', "Other regular income or expense on an account."),
        ('b', "One-off or irregular amounts on an account.")) switch
    {
        0 => CreateEmployment(config),
        1 => CreateDeductible(config),
        2 => CreateSuperContribution(config),
        3 => CreateGenericFlow(config),
        4 => CreateBursts(config),
        _ => null
    };
}

static Contract? CreateEmployment(Configuration config)
{
    var individual = SelectIndividual(config, "Whose employment is it:");
    if (individual is null)
    {
        return null;
    }
    var cashAccount = SelectAccount(config, "Account the pay goes into:");
    if (cashAccount is null)
    {
        return null;
    }

    var descriptor = ReadFlowDescriptor("Current yearly gross salary:", true);
    var employment = new Employment(descriptor, individual, cashAccount)
    {
        WithholdPayg = ReadYesOrNoUntilAnswered("Does the employer withhold PAYG"),
        PaygWithholdingFrequency = TimeSpan.FromDays(ReadPositiveInt("Paid every how many days:", 14))
    }.CreateAllNaturalTrackerKeys();
    employment.Name = Answer("Name of the contract:", employment.Name);

    individual.TaxableContracts.Add(employment);
    return employment;
}

static Contract? CreateDeductible(Configuration config)
{
    var individual = SelectIndividual(config, "Who claims the deduction:");
    if (individual is null)
    {
        return null;
    }
    var cashAccount = SelectAccount(config, "Account the expense is paid from:");
    if (cashAccount is null)
    {
        return null;
    }

    var name = AnswerUntilAnswered("Name of the deductible expense:");
    // A deductible is an outflow, so its flow is negative and the tax accounting negates it back.
    var descriptor = ReadFlowDescriptor("Current yearly amount:", false);
    var deductible = new Deductible(descriptor, individual, cashAccount, name).CreateAllNaturalTrackerKeys();

    individual.TaxableContracts.Add(deductible);
    return deductible;
}

static Contract? CreateSuperContribution(Configuration config)
{
    var cashAccount = SelectAccount(config, "Account the contribution is paid from:");
    if (cashAccount is null)
    {
        return null;
    }

    var descriptor = ReadFlowDescriptor("Current yearly contribution:", false);
    var contribution = new SuperContribution(descriptor, cashAccount).CreateAllNaturalTrackerKeys();
    contribution.Name = Answer("Name of the contract:", contribution.Name);

    var individual = SelectIndividual(config, "Whose contribution is it (leave it blank if it is not tied to an individual):");
    individual?.TaxableContracts.Add(contribution);

    return contribution;
}

static Contract? CreateGenericFlow(Configuration config)
{
    var cashAccount = SelectAccount(config, "Account the flow is applied to:");
    if (cashAccount is null)
    {
        return null;
    }

    var name = AnswerUntilAnswered("Name of the flow:");
    var isInflow = ReadOptionsUntilAnswered("Is it an income or an expense", ('i', "Income."), ('e', "Expense.")) == 0;
    var descriptor = ReadFlowDescriptor(isInflow ? "Current yearly income:" : "Current yearly expense:", isInflow);

    return new BandedFlow(descriptor, cashAccount, name).CreateAllNaturalTrackerKeys();
}

static Contract? CreateBursts(Configuration config)
{
    var cashAccount = SelectAccount(config, "Account the amounts are applied to:");
    if (cashAccount is null)
    {
        return null;
    }

    var name = AnswerUntilAnswered("Name of the amounts:");
    var amounts = new List<(DateTime Time, decimal Amount)>();
    while (true)
    {
        var time = ReadOptionalDate(amounts.Count == 0 ? "Date of the first amount" : "Date of the next amount");
        if (time is null)
        {
            break;
        }
        var amount = ReadDecimal("Amount (negative for money going out):", 0m);
        amounts.Add((time.Value, amount));
    }

    if (amounts.Count == 0)
    {
        Console.WriteLine("No amount has been entered, so nothing is added.");
        return null;
    }

    // Bursts walks its list in order and starts at the first entry, so the two have to line up.
    amounts.Sort((x, y) => x.Time.CompareTo(y.Time));

    var bursts = new Bursts(amounts[0].Time, cashAccount, name).CreateAllNaturalTrackerKeys();
    bursts.BurstsList.AddRange(amounts);
    return bursts;
}

static void PrintConfigurationSummary(Configuration config)
{
    Console.WriteLine();
    Console.WriteLine("=== Configuration ===");

    Console.WriteLine($"{config.TaxIndividuals.Count} tax individual(s):");
    foreach (var individual in config.TaxIndividuals)
    {
        var family = individual.Family is null ? "no family" : $"family '{individual.Family.Name}'";
        var tax = individual.Tax is null ? "no tax assessment" : "tax assessment set up";
        Console.WriteLine($" {individual.Name}: {family}, {individual.TaxableContracts.Count} taxable contract(s), {tax}");
    }

    Console.WriteLine($"{config.Families.Count} family/families:");
    foreach (var family in config.Families)
    {
        Console.WriteLine($" {family.Name}: {family.TaxMembers.Count} member(s), {family.DependencyCount} dependency/dependencies");
    }

    Console.WriteLine($"{config.Accounts.Count} account(s):");
    foreach (var account in config.Accounts)
    {
        var owners = account.Ownership.Count == 0
            ? "no owner"
            : string.Join(", ", account.Ownership.Select(x => $"{x.Entity.Name} {x.Share:P2}"));
        Console.WriteLine($" {DescribeAccount(account)}: {owners}");
    }

    Console.WriteLine($"{config.ExistingContracts.Count} existing contract(s):");
    foreach (var contract in config.ExistingContracts)
    {
        Console.WriteLine($" {contract.Name}");
    }

    Console.WriteLine($"{config.OptionalContracts.Count} optional contract(s):");
    foreach (var (contract, enabled) in config.OptionalContracts)
    {
        Console.WriteLine($" {contract.Name} ({(enabled ? "enabled" : "disabled")} by default)");
    }

    Console.WriteLine();
}
