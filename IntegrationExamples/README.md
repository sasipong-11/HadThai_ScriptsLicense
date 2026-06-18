# Integration Example

Use `HadthaiLicense/HadthaiLicense.csproj` as the reusable library for your application.

## What To Copy Or Reference

- Preferred: reference the `HadthaiLicense` project directly
- Alternative: copy `HadthaiLicense/HadthaiLicenseClient.cs` into your application

## Minimal Usage

```csharp
using HadthaiLicense;

using var httpClient = new HttpClient();
var client = new HadthaiLicenseClient("https://ld-api.uat.rtt.in.th", httpClient);

string licenseKey = "X0OZ5-033Y7-ZEIYH-9VS11-00001";
string uid = "YOUR_DEVICE_UID";

int validateResult = await client.CheckLicenseHadthaiAvailableAsync(licenseKey, uid);
int activateResult = await client.CheckLicenseHadthaiInstallAsync(licenseKey, uid);
```

## Return Codes

- `1`: success
- `2`: invalid license or validation failed
- `3`: network or unexpected error
- `4`: license belongs to another device

## Notes

- License keys can include or omit hyphens.
- Your application must provide a real `uid`.
- `CalculateUid()` is still a placeholder and should be implemented by the consuming application if needed.
