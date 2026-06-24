using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HadthaiLicense;

// Return codes:
// 1 = success
// 2 = invalid license / validation failed
// 3 = network or unexpected error
// 4 = license belongs to another device
public sealed class HadthaiLicenseClient
{
    private const string ProductVersion = "1";
    private const string ActivationStoreDirectoryName = "HadthaiLicense";
    private const string ActivationRegistryFileName = "activation-registry.txt";
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _enforceCurrentMachineUid;
    private readonly bool _simulateBackendLock;

    public HadthaiLicenseClient(
        string baseUrl,
        HttpClient? httpClient = null,
        bool enforceCurrentMachineUid = true,
        bool simulateBackendLock = false)
    {
        _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
        _enforceCurrentMachineUid = enforceCurrentMachineUid;
        _simulateBackendLock = simulateBackendLock;
    }

    public async Task<int> CheckLicenseHadthaiAvailableAsync(
        string keyNum,
        string uid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string normalizedKey = NormalizeLicenseKey(keyNum);
            string currentMachineUid = CalculateUid();
            string effectiveUid = string.IsNullOrWhiteSpace(uid) ? currentMachineUid : uid.Trim();

            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(effectiveUid))
            {
                return 2;
            }

            if (_enforceCurrentMachineUid &&
                !string.Equals(effectiveUid, currentMachineUid, StringComparison.Ordinal))
            {
                return 4;
            }

            if (!IsStoredBindingValid(normalizedKey, effectiveUid))
            {
                return 4;
            }

            if (_simulateBackendLock && !IsActivationRegistryValid(normalizedKey, effectiveUid))
            {
                return 4;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/api/license/validate-product?prgVersion={ProductVersion}");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["keyNum"] = normalizedKey,
                ["uid"] = effectiveUid,
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            string apiMessage = GetLicenseApiMessage(content);

            if (apiMessage == "OK")
            {
                return 1;
            }

            if (apiMessage == "CHANGE_SERIAL")
            {
                return 4;
            }

            return 2;
        }
        catch
        {
            return 3;
        }
    }

    public async Task<int> CheckLicenseHadthaiInstallAsync(
        string keyNum,
        string uid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string normalizedKey = NormalizeLicenseKey(keyNum);
            string currentMachineUid = CalculateUid();
            string effectiveUid = string.IsNullOrWhiteSpace(uid) ? currentMachineUid : uid.Trim();

            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(effectiveUid))
            {
                return 2;
            }

            if (_enforceCurrentMachineUid &&
                !string.Equals(effectiveUid, currentMachineUid, StringComparison.Ordinal))
            {
                return 4;
            }

            if (!IsStoredBindingValid(normalizedKey, effectiveUid))
            {
                return 4;
            }

            if (_simulateBackendLock && !TryRegisterActivation(normalizedKey, effectiveUid))
            {
                return 4;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/api/license/activate-product?prgVersion={ProductVersion}");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["keyNum"] = normalizedKey,
                ["uid"] = effectiveUid,
                ["fullname"] = string.Empty,
                ["organizationName"] = string.Empty,
                ["email"] = string.Empty,
                ["phoneNo"] = string.Empty,
            });

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            string apiMessage = GetLicenseApiMessage(content);

            if (apiMessage == "OK")
            {
                SaveLicenseBinding(normalizedKey, effectiveUid);
                return 1;
            }

            if (apiMessage == "CHANGE_SERIAL")
            {
                return 4;
            }

            return 2;
        }
        catch
        {
            return 3;
        }
    }

    public static string NormalizeLicenseKey(string keyNum)
    {
        if (string.IsNullOrWhiteSpace(keyNum))
        {
            return string.Empty;
        }

        return Regex.Replace(keyNum.ToUpperInvariant(), "[^A-Z0-9]", "");
    }

    public static string CalculateUid()
    {
        string fingerprint = GetMachineFingerprint();
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return string.Empty;
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToHexString(hash);
    }

    public static string GetLicenseApiMessage(string responseContent)
    {
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return string.Empty;
        }

        Match match = Regex.Match(
            responseContent,
            "\"msg\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim().ToUpperInvariant();
        }

        return responseContent.Trim().ToUpperInvariant();
    }

    public static string GetActivationBindingPath(string keyNum)
    {
        string normalizedKey = NormalizeLicenseKey(keyNum);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return string.Empty;
        }

        return GetBindingFilePath(normalizedKey);
    }

    public static bool ClearStoredLicenseBinding(string keyNum)
    {
        string normalizedKey = NormalizeLicenseKey(keyNum);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return false;
        }

        string path = GetBindingFilePath(normalizedKey);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public static string GetActivationRegistryPath()
    {
        return Path.Combine(GetStorageDirectory(), ActivationRegistryFileName);
    }

    public static bool ClearActivationRegistry()
    {
        string path = GetActivationRegistryPath();
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    private static string GetMachineFingerprint()
    {
        string machineGuid = GetWindowsMachineGuid();
        if (!string.IsNullOrWhiteSpace(machineGuid))
        {
            return machineGuid;
        }

        return string.Join(
            "|",
            Environment.MachineName,
            Environment.UserDomainName,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString());
    }

    private static string GetWindowsMachineGuid()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        try
        {
            object? value = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid",
                null);

            return value?.ToString()?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsStoredBindingValid(string normalizedKey, string uid)
    {
        string path = GetBindingFilePath(normalizedKey);
        if (!File.Exists(path))
        {
            return true;
        }

        string storedUid = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(storedUid))
        {
            return false;
        }

        return string.Equals(storedUid, uid, StringComparison.Ordinal);
    }

    private static void SaveLicenseBinding(string normalizedKey, string uid)
    {
        string path = GetBindingFilePath(normalizedKey);
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, uid);
    }

    private static string GetBindingFilePath(string normalizedKey)
    {
        string licenseHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalizedKey)));

        return Path.Combine(GetStorageDirectory(), $"binding-{licenseHash}.txt");
    }

    private static string GetStorageDirectory()
    {
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(appDataDirectory)
            ? Path.Combine(Path.GetTempPath(), ActivationStoreDirectoryName)
            : Path.Combine(appDataDirectory, ActivationStoreDirectoryName);
    }

    private static bool IsActivationRegistryValid(string normalizedKey, string uid)
    {
        Dictionary<string, string> entries = LoadActivationRegistry();
        return !entries.TryGetValue(normalizedKey, out string? registeredUid)
            || string.Equals(registeredUid, uid, StringComparison.Ordinal);
    }

    private static bool TryRegisterActivation(string normalizedKey, string uid)
    {
        Dictionary<string, string> entries = LoadActivationRegistry();
        if (entries.TryGetValue(normalizedKey, out string? registeredUid))
        {
            return string.Equals(registeredUid, uid, StringComparison.Ordinal);
        }

        entries[normalizedKey] = uid;
        SaveActivationRegistry(entries);
        return true;
    }

    private static Dictionary<string, string> LoadActivationRegistry()
    {
        string path = GetActivationRegistryPath();
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return entries;
        }

        foreach (string line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split('|', 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                continue;
            }

            string normalizedKey = parts[0].Trim();
            string uid = parts[1].Trim();
            if (!string.IsNullOrWhiteSpace(normalizedKey) && !string.IsNullOrWhiteSpace(uid))
            {
                entries[normalizedKey] = uid;
            }
        }

        return entries;
    }

    private static void SaveActivationRegistry(Dictionary<string, string> entries)
    {
        string directory = GetStorageDirectory();
        Directory.CreateDirectory(directory);

        string path = GetActivationRegistryPath();
        var builder = new StringBuilder();

        foreach (KeyValuePair<string, string> entry in entries)
        {
            builder.Append(entry.Key)
                .Append('|')
                .Append(entry.Value)
                .AppendLine();
        }

        File.WriteAllText(path, builder.ToString());
    }
}
