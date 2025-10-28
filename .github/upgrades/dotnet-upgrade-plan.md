# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade iSpyMonitor.csproj
4. Upgrade WixCA.csproj
5. Upgrade iSpy.csproj

## Settings

This section contains settings and data used by execution steps.

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### iSpyMonitor.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.7.2` to `net8.0-windows`

#### WixCA.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net8.0`

#### iSpy.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.7.2` to `net8.0-windows`

NuGet packages changes:
  - Microsoft.Bcl version 1.1.10 to be removed (deprecated)
  - Microsoft.Bcl.Async version 1.0.168 to be removed (deprecated)
  - Microsoft.Bcl.AsyncInterfaces version 10.0.0-rc.2.25502.107 to be replaced with version 8.0.0
  - Microsoft.Net.Http version 2.2.29 to be replaced with System.Net.Http version 4.3.4
  - Mono.Security version 3.2.3.0 to be removed (no supported version found)
  - PCLCrypto version 1.0.0.15071 to be removed (no supported version found)
  - System.IO.Pipelines version 10.0.0-rc.2.25502.107 to be replaced with version 8.0.0
  - System.Text.Encodings.Web version 10.0.0-rc.2.25502.107 to be replaced with version 8.0.0
  - System.Text.Json version 10.0.0-rc.2.25502.107 to be replaced with version 8.0.6
  - Validation version 2.0.6.15003 to be updated to version 2.6.68
  - Zlib.Portable version 1.11.0 to be removed (no supported version found)
  - Zlib.Portable.Signed version 1.11.0 to be removed (no supported version found)

Following packages will be removed as their functionality is included in the .NET 8.0 framework:
  - System.Buffers version 4.6.1
  - System.IO version 4.3.0
  - System.Memory version 4.6.3
  - System.Net.Http version 4.3.4
  - System.Numerics.Vectors version 4.6.1
  - System.Runtime version 4.3.1
  - System.Security.Cryptography.Algorithms version 4.3.1
  - System.Security.Cryptography.Encoding version 4.3.0
  - System.Security.Cryptography.Primitives version 4.3.0
  - System.Security.Cryptography.X509Certificates version 4.3.2
  - System.Text.RegularExpressions version 4.3.1
  - System.Threading.Tasks.Extensions version 4.6.3
  - System.ValueTuple version 4.6.1

Feature upgrades:
  - WCF services need to be migrated to CoreWCF