param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Entrega\Actividad-1-Proceso-Extraccion.zip')
)

$resolvedRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$expectedOutputDirectory = [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot 'Entrega'))

if (-not $resolvedOutput.StartsWith($expectedOutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'El archivo de salida debe permanecer dentro de la carpeta Entrega del proyecto.'
}

New-Item -ItemType Directory -Path $expectedOutputDirectory -Force | Out-Null

if (Test-Path -LiteralPath $resolvedOutput) {
    Remove-Item -LiteralPath $resolvedOutput -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$excludedDirectories = @('.dotnet-cli', 'bin', 'obj', 'tmp', 'Entrega')
$files = Get-ChildItem -LiteralPath $resolvedRoot -File -Recurse | Where-Object {
    $relativePath = [System.IO.Path]::GetRelativePath($resolvedRoot, $_.FullName)
    $segments = $relativePath -split '[\\/]'
    -not ($segments | Where-Object { $excludedDirectories -contains $_ })
}

$archive = [System.IO.Compression.ZipFile]::Open($resolvedOutput, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relativePath = [System.IO.Path]::GetRelativePath($resolvedRoot, $file.FullName).Replace('\', '/')
        $entryName = "AnalisisOpiniones-Extraction/$relativePath"
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $file.FullName,
            $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

$package = Get-Item -LiteralPath $resolvedOutput
[PSCustomObject]@{
    Archivo = $package.FullName
    ArchivosIncluidos = $files.Count
    TamanoBytes = $package.Length
    SHA256 = (Get-FileHash -LiteralPath $resolvedOutput -Algorithm SHA256).Hash
}
