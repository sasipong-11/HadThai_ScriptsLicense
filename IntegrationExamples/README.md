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
string uid = HadthaiLicenseClient.CalculateUid();

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
- `CalculateUid()` now derives a stable machine UID from the current device.
- Do not send a `uid` from another machine. If the supplied `uid` does not match the current device, the client returns `4`.
- After activation succeeds, the library stores a local binding for that license and machine. If the stored binding and current machine do not match, the client returns `4`.
- For demos, the harness can simulate a backend-side activation registry and reject the same license on a second `uid`.
