# MU BMD Decryptor
# Decrypts a BMD file using multiple known XOR keys and saves results to compare.
# User checks which output is readable text to find the right key.

param(
    [string]$InputFile = "C:\MuDev\BarnaMu-Client\Data\Local\Eng\movereq_eng.bmd",
    [string]$OutputPrefix = "C:\MuDev\movereq_decrypted"
)

if (-not (Test-Path $InputFile)) {
    Write-Host "ERROR: File not found: $InputFile" -ForegroundColor Red
    exit 1
}

$bytes = [System.IO.File]::ReadAllBytes($InputFile)
Write-Host "File size: $($bytes.Length) bytes"
Write-Host "First 8 bytes (hex): $($bytes[0..7] | ForEach-Object { '{0:X2}' -f $_ } | Join-String -Separator ' ')"

# Known MU BMD XOR keys (16 bytes each). Different MU versions use different keys.
$candidateKeys = @(
    # Key 1 - Standard MU BMD key (most common in S6)
    @(0xFC, 0xCF, 0xAB, 0xBA, 0xFA, 0xAB, 0xCD, 0x12, 0x23, 0x34, 0x45, 0x56, 0x67, 0x78, 0x89, 0x9A),
    # Key 2 - Alternative (some custom builds)
    @(0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27, 0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2),
    # Key 3 - Older Webzen format
    @(0xAB, 0x11, 0xCD, 0xFE, 0x18, 0x23, 0xC5, 0xA3, 0xCA, 0x33, 0xAA, 0x55, 0xAF, 0x55, 0x50, 0xBC),
    # Key 4 - MuOnline.cz / community key
    @(0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27, 0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2)
)

# Try with NO header skip and with 4-byte header skip
$skipOptions = @(0, 4, 8)

$keyIndex = 0
foreach ($key in $candidateKeys) {
    foreach ($skipBytes in $skipOptions) {
        $bodyStart = $skipBytes
        $bodyLength = $bytes.Length - $bodyStart
        if ($bodyLength -le 0) { continue }

        $decrypted = New-Object byte[] $bodyLength
        for ($i = 0; $i -lt $bodyLength; $i++) {
            $decrypted[$i] = $bytes[$bodyStart + $i] -bxor $key[$i % $key.Length]
        }

        $outFile = "$OutputPrefix-key$keyIndex-skip$skipBytes.txt"
        [System.IO.File]::WriteAllBytes($outFile, $decrypted)

        # Quick check: how many bytes are printable ASCII?
        $printable = 0
        $totalChecked = [Math]::Min(500, $decrypted.Length)
        for ($i = 0; $i -lt $totalChecked; $i++) {
            $b = $decrypted[$i]
            if (($b -ge 0x20 -and $b -le 0x7E) -or $b -eq 0x0A -or $b -eq 0x0D -or $b -eq 0x09) {
                $printable++
            }
        }
        $printableRatio = [Math]::Round($printable / $totalChecked * 100, 1)
        Write-Host "Key $keyIndex, skip $skipBytes bytes -> $outFile  (printable: $printableRatio%)"
    }
    $keyIndex++
}

Write-Host ""
Write-Host "Open each .txt file with notepad. The one with mostly readable text is the right key."
Write-Host "Look for: map names like 'Lorencia', 'Devias', 'Atlans' etc."
