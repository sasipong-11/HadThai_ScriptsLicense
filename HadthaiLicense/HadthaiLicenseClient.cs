using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HadthaiLicense;

// Return codes:
// 1 = success
// 2 = invalid license / validation failed
// 3 = network or unexpected error
// 4 = license belongs to another device
public sealed class HadthaiLicenseClient
{
    private const string ProductVersion = "1";
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public HadthaiLicenseClient(string baseUrl, HttpClient? httpClient = null)
    {
        _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<int> CheckLicenseHadthaiAvailableAsync(
        string keyNum,
        string uid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string normalizedKey = NormalizeLicenseKey(keyNum);
            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(uid))
            {
                return 2;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/api/license/validate-product?prgVersion={ProductVersion}");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["keyNum"] = normalizedKey,
                ["uid"] = uid,
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
            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(uid))
            {
                return 2;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_baseUrl}/api/license/activate-product?prgVersion={ProductVersion}");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["keyNum"] = normalizedKey,
                ["uid"] = uid,
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
        return string.Empty;
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
}
