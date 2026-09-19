# Runs the engine-independent networking rules with the Windows .NET Framework compiler.
# This does not compile Unity code, run a Unity scene, or validate an Android build.
$ErrorActionPreference = 'Stop'
$gameRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$checkDir = Join-Path $gameRoot '.validation'
New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'The Windows .NET Framework C# compiler was not found. Run the Unity EditMode tests instead.' }
$checkSource = @'
using System;
using System.Text;
using BrothersBlock;
public static class LanSmokeCheck
{
    private static int count;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        count++;
    }
    public static int Main()
    {
        string parsed;
        foreach (string valid in new[] { "192.168.1.20", "10.0.0.5", "172.16.1.1", "127.0.0.1" })
            Check(LanRules.TryAddress(valid, out parsed) && parsed == valid, "Valid address " + valid);
        Check(LanRules.TryAddress(" 192.168.1.20 ", out parsed) && parsed == "192.168.1.20", "Whitespace trim");
        foreach (string invalid in new[] { null, "", "127.1", "192.168.1.999", "192.168.1.20:7777", "0.0.0.0", "255.255.255.255", "224.0.0.1", "::1", "https://192.168.1.20", "192.168.001.010", "192.168.1.-1", "0x7f.0.0.1" })
            Check(!LanRules.TryAddress(invalid, out parsed), "Invalid address " + invalid);
        byte[] payload = Encoding.UTF8.GetBytes(LanRules.Protocol);
        Check(LanRules.Refusal(0, payload) == "", "Host admitted");
        Check(LanRules.Refusal(1, payload) == "", "Brother admitted");
        Check(LanRules.Refusal(2, payload).Contains("two players"), "Third player refused");
        Check(LanRules.Refusal(0, null).Length > 0, "Missing protocol refused");
        Check(LanRules.Refusal(0, Encoding.UTF8.GetBytes("old-game")).Length > 0, "Different protocol refused");
        Check(LanRules.Refusal(0, new byte[1024]).Length > 0, "Oversized handshake refused");
        Console.WriteLine("PASS: " + count + " engine-independent LAN checks. Unity and device testing are still required.");
        return 0;
    }
}
'@
$checkPath = Join-Path $checkDir 'LanSmokeCheck.cs'
$binaryPath = Join-Path $checkDir 'LanSmokeCheck.exe'
[IO.File]::WriteAllText($checkPath, $checkSource)
& $compilerPath /nologo /target:exe "/out:$binaryPath" (Join-Path $gameRoot 'Assets/Scripts/LanRules.cs') $checkPath
if ($LASTEXITCODE -ne 0) { throw 'LAN rule compilation failed.' }
& $binaryPath
if ($LASTEXITCODE -ne 0) { throw 'LAN rule checks failed.' }
