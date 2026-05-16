# MU BMD Decryptor v2
# Wider set of known XOR keys + known-plaintext attack to derive the actual key

param(
    [string]$InputFile = "C:\MuDev\BarnaMu-Client\Data\Local\Eng\movereq_eng.bmd",
    [string]$OutputPrefix = "C:\MuDev\movereq_decrypted"
)

if (-not (Test-Path $InputFile)) {
    Write-Host "ERROR: File not found: $InputFile"
    exit 1
}

$bytes = [System.IO.File]::ReadAllBytes($InputFile)
Write-Host "File size: $($bytes.Length) bytes"

# Print first 32 bytes hex (fix Join-String issue)
$hexStr = ""
for ($i = 0; $i -lt [Math]::Min(32, $bytes.Length); $i++) {
    $hexStr += "{0:X2} " -f $bytes[$i]
}
Write-Host "First 32 bytes: $hexStr"
Write-Host ""

# Expanded set of MU BMD keys (from various MU versions / community)
$candidateKeys = @(
    @{ name="MU_Standard_S6";        bytes=@(0xFC, 0xCF, 0xAB, 0xBA, 0xFA, 0xAB, 0xCD, 0x12, 0x23, 0x34, 0x45, 0x56, 0x67, 0x78, 0x89, 0x9A) },
    @{ name="MU_Alt_S4";              bytes=@(0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27, 0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2) },
    @{ name="MU_Old_Webzen";          bytes=@(0xAB, 0x11, 0xCD, 0xFE, 0x18, 0x23, 0xC5, 0xA3, 0xCA, 0x33, 0xAA, 0x55, 0xAF, 0x55, 0x50, 0xBC) },
    @{ name="MU_S6_v2";               bytes=@(0xF8, 0x90, 0x36, 0x4E, 0xF2, 0x5A, 0x6B, 0xFA, 0x89, 0xEE, 0xF5, 0x24, 0x89, 0x6D, 0x32, 0x0F) },
    @{ name="MU_Custom_1";            bytes=@(0xD3, 0x96, 0x68, 0xE1, 0xA2, 0xC8, 0xB4, 0x71, 0xA0, 0xC8, 0xC5, 0x6D, 0x6F, 0xD7, 0x9F, 0x55) },
    @{ name="MU_Custom_2";            bytes=@(0xD0, 0x4F, 0x35, 0x68, 0x6B, 0x9B, 0x67, 0x91, 0x33, 0xD5, 0x99, 0x4F, 0x21, 0x96, 0x4C, 0xE2) },
    @{ name="MU_ServerFile";          bytes=@(0xAB, 0xCF, 0xBA, 0xFA, 0xAB, 0xCD, 0x12, 0x23, 0x34, 0x45, 0x56, 0x67, 0x78, 0x89, 0x9A, 0xFC) }
)

$skipOptions = @(0, 1, 2, 3, 4, 8, 16)

# Function to count "looks like english text" score
function Get-ReadabilityScore([byte[]]$data, [int]$sampleSize = 500) {
    $checked = [Math]::Min($sampleSize, $data.Length)
    if ($checked -le 0) { return 0 }
    $printable = 0
    $letters = 0
    for ($i = 0; $i -lt $checked; $i++) {
        $b = $data[$i]
        if (($b -ge 0x20 -and $b -le 0x7E) -or $b -eq 0x0A -or $b -eq 0x0D -or $b -eq 0x09 -or $b -eq 0x00) {
            $printable++
        }
        if (($b -ge 0x41 -and $b -le 0x5A) -or ($b -ge 0x61 -and $b -le 0x7A)) {
            $letters++
        }
    }
    return @{
        printableRatio = [Math]::Round($printable * 100.0 / $checked, 1)
        letterRatio = [Math]::Round($letters * 100.0 / $checked, 1)
    }
}

# Function to check if "Lorencia" or other map names appear (definitive proof)
function Test-ContainsMapNames([byte[]]$data) {
    $text = [System.Text.Encoding]::ASCII.GetString($data)
    $names = @("Lorencia", "Devias", "Atlans", "Noria", "Dungeon", "Tarkan", "Aida")
    $found = @()
    foreach ($name in $names) {
        if ($text -match $name) {
            $found += $name
        }
    }
    return $found
}

Write-Host "Trying all key/skip combinations..." -ForegroundColor Cyan
Write-Host ""

$best = @{ score = 0; name = ""; skip = 0; mapsFound = @() }

foreach ($keyDef in $candidateKeys) {
    foreach ($skip in $skipOptions) {
        $bodyLen = $bytes.Length - $skip
        if ($bodyLen -le 0) { continue }

        $decrypted = New-Object byte[] $bodyLen
        for ($i = 0; $i -lt $bodyLen; $i++) {
            $decrypted[$i] = $bytes[$skip + $i] -bxor $keyDef.bytes[$i % $keyDef.bytes.Length]
        }

        $outFile = "$OutputPrefix-$($keyDef.name)-skip$skip.txt"
        [System.IO.File]::WriteAllBytes($outFile, $decrypted)

        $score = Get-ReadabilityScore $decrypted
        $maps = Test-ContainsMapNames $decrypted

        $marker = ""
        if ($maps.Count -gt 0) {
            $marker = "  *** FOUND MAPS: $($maps -join ', ') ***"
        }

        Write-Host ("Key={0,-20} skip={1,2}  printable={2,5}%  letters={3,5}%{4}" -f $keyDef.name, $skip, $score.printableRatio, $score.letterRatio, $marker)

        $combined = $score.printableRatio + $maps.Count * 50
        if ($combined -gt $best.score) {
            $best = @{ score = $combined; name = $keyDef.name; skip = $skip; mapsFound = $maps }
        }
    }
}

Write-Host ""
Write-Host "===========================================" -ForegroundColor Yellow
Write-Host "BEST: Key='$($best.name)', skip=$($best.skip)" -ForegroundColor Yellow
if ($best.mapsFound.Count -gt 0) {
    Write-Host "Maps found: $($best.mapsFound -join ', ')" -ForegroundColor Green
    Write-Host "Open: $OutputPrefix-$($best.name)-skip$($best.skip).txt" -ForegroundColor Green
} else {
    Write-Host "No map names found in any combination. We need to derive the key from known plaintext." -ForegroundColor Red
    Write-Host ""
    Write-Host "Doing known-plaintext attack: assuming 'Lorencia' appears in the file..."

    # Try every position: assume "Lorencia" or "LORENCIA" or "lorencia" is at byte N
    $plaintexts = @("Lorencia", "LORENCIA", "lorencia", "Lorencia`0")
    foreach ($pt in $plaintexts) {
        $ptBytes = [System.Text.Encoding]::ASCII.GetBytes($pt)
        # Slide through the file
        for ($pos = 0; $pos -le $bytes.Length - $ptBytes.Length; $pos++) {
            # Derive what the key would be if plaintext at $pos is $pt
            $derivedKey = New-Object byte[] 16
            $keyConsistent = $true
            for ($i = 0; $i -lt $ptBytes.Length; $i++) {
                $keyIdx = ($pos + $i) % 16
                $b = $bytes[$pos + $i] -bxor $ptBytes[$i]
                if ($derivedKey[$keyIdx] -eq 0 -or $i -lt 16) {
                    $derivedKey[$keyIdx] = $b
                } elseif ($derivedKey[$keyIdx] -ne $b) {
                    $keyConsistent = $false
                    break
                }
            }
            if ($keyConsistent -and $ptBytes.Length -ge 8) {
                Write-Host "Possible key match at pos=$pos for '$pt'" -ForegroundColor Green
                $hex = ""
                for ($k = 0; $k -lt 16; $k++) { $hex += "{0:X2} " -f $derivedKey[$k] }
                Write-Host "  Partial derived key: $hex" -ForegroundColor Green
            }
        }
    }
}
