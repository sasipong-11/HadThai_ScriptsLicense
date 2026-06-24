using HadthaiLicense;
using System.Net.Http;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var options = ParseArgs(args);

        if (options.ContainsKey("help") || options.ContainsKey("?"))
        {
            PrintUsage();
            return 0;
        }

        string baseUrl = GetOption(options, "baseUrl", "https://ld-api.uat.rtt.in.th");
        string mode = GetOption(options, "mode", Prompt("Mode (validate/activate/showuid/release/resetregistry)", "validate")).Trim().ToLowerInvariant();
        bool allowUidOverride = string.Equals(GetOption(options, "allowUidOverride", "false"), "true", StringComparison.OrdinalIgnoreCase);
        bool simulateBackendLock = string.Equals(GetOption(options, "simulateBackendLock", "false"), "true", StringComparison.OrdinalIgnoreCase);

        if (mode == "showuid")
        {
            Console.WriteLine($"UID: {HadthaiLicenseClient.CalculateUid()}");
            return 0;
        }

        if (mode == "resetregistry")
        {
            bool removed = HadthaiLicenseClient.ClearActivationRegistry();
            Console.WriteLine(removed
                ? "Simulated activation registry removed."
                : "No simulated activation registry found.");
            return removed ? 0 : 1;
        }

        string keyNum = HadthaiLicenseClient.NormalizeLicenseKey(
            GetOption(options, "key", Prompt("License key", string.Empty)));

        if (mode == "release")
        {
            bool removed = HadthaiLicenseClient.ClearStoredLicenseBinding(keyNum);
            Console.WriteLine(removed
                ? "Stored license binding removed."
                : "No stored license binding found.");
            return removed ? 0 : 1;
        }

        string uid = GetOption(options, "uid", HadthaiLicenseClient.CalculateUid());

        if (string.IsNullOrWhiteSpace(uid))
        {
            uid = Prompt("UID", string.Empty);
        }

        using var httpClient = new HttpClient();
        var client = new HadthaiLicenseClient(
            baseUrl,
            httpClient,
            enforceCurrentMachineUid: !allowUidOverride,
            simulateBackendLock: simulateBackendLock);

        Console.WriteLine($"Base URL: {baseUrl}");
        Console.WriteLine($"Mode: {mode}");
        Console.WriteLine($"UID: {uid}");
        Console.WriteLine($"Allow UID Override: {allowUidOverride}");
        Console.WriteLine($"Simulate Backend Lock: {simulateBackendLock}");
        Console.WriteLine($"Binding Path: {HadthaiLicenseClient.GetActivationBindingPath(keyNum)}");
        Console.WriteLine($"Registry Path: {HadthaiLicenseClient.GetActivationRegistryPath()}");

        int result = mode == "activate"
            ? await client.CheckLicenseHadthaiInstallAsync(keyNum, uid)
            : await client.CheckLicenseHadthaiAvailableAsync(keyNum, uid);

        Console.WriteLine();
        Console.WriteLine($"Result code: {result}");
        Console.WriteLine(DescribeResult(result));

        return result == 1 ? 0 : 1;
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            int separatorIndex = arg.IndexOf('=');
            if (separatorIndex < 0)
            {
                options[arg[2..]] = "true";
                continue;
            }

            string key = arg[2..separatorIndex];
            string value = arg[(separatorIndex + 1)..];
            options[key] = value;
        }

        return options;
    }

    private static string GetOption(Dictionary<string, string> options, string key, string fallback)
    {
        return options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string Prompt(string label, string defaultValue)
    {
        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            Console.Write($"{label} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{label}: ");
        }

        string? value = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim();
    }

    private static string DescribeResult(int result)
    {
        return result switch
        {
            1 => "Success",
            2 => "Invalid license or validation failed",
            3 => "Network or unexpected error",
            4 => "License belongs to another device",
            _ => "Unknown result",
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Hadthai license harness");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --mode=validate|activate|showuid|release|resetregistry");
        Console.WriteLine("  --baseUrl=https://ld-api.uat.rtt.in.th");
        Console.WriteLine("  --key=ABCDE12345FGHIJ67890KLMN");
        Console.WriteLine("    Hyphens are optional. Example: ABCDE-12345-FGHIJ-67890-KLMN");
        Console.WriteLine("  --uid=<device uid>");
        Console.WriteLine("  --allowUidOverride=true|false");
        Console.WriteLine("  --simulateBackendLock=true|false");
    }
}
