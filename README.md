# Hadthai License Client

Reusable C# library plus a small console harness for Hadthai license activation and validation.

## Files

- `HadthaiLicense/HadthaiLicense.csproj`
- `HadthaiLicense/HadthaiLicenseClient.cs`
- `HadthaiLicenseHarness/HadthaiLicenseHarness.csproj`
- `HadthaiLicenseHarness/Program.cs`
- `IntegrationExamples/README.md`

## API Contract

- Default base URL:
  - `https://ld-api.uat.rtt.in.th`
- Validate:
  - `POST /api/license/validate-product?prgVersion=1`
- Activate:
  - `POST /api/license/activate-product?prgVersion=1`

## API Requests

By default, the console harness sends requests to:

- `https://ld-api.uat.rtt.in.th/api/license/validate-product?prgVersion=1`
- `https://ld-api.uat.rtt.in.th/api/license/activate-product?prgVersion=1`

You can override the base URL with:

```bash
--baseUrl=https://your-api-host
```

### Validate Request

- Method: `POST`
- URL: `{baseUrl}/api/license/validate-product?prgVersion=1`
- Form fields:
  - `keyNum`
  - `uid`

### Activate Request

- Method: `POST`
- URL: `{baseUrl}/api/license/activate-product?prgVersion=1`
- Form fields:
  - `keyNum`
  - `uid`

The script only requires:

- license key
- uid

Contact fields are sent as empty values during activation.
License keys can be provided with or without hyphens. The script normalizes them automatically before sending to the API.

## Recommended Handoff

If another application team needs to integrate this feature, give them:

- `HadthaiLicense/HadthaiLicense.csproj` for direct project reference
- `HadthaiLicense/HadthaiLicenseClient.cs` as the actual implementation
- `IntegrationExamples/README.md` as the usage guide

The console harness is mainly for manual testing and troubleshooting.

## Run

```bash
dotnet run --project HadthaiLicenseHarness -- --mode=validate --key=YOUR_LICENSE_KEY --uid=YOUR_DEVICE_UID
```

```bash
dotnet run --project HadthaiLicenseHarness -- --mode=activate --key=YOUR_LICENSE_KEY --uid=YOUR_DEVICE_UID
```
