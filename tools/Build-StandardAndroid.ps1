param([switch]$InstallDependencies)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$dotnet = Join-Path $PSScriptRoot 'dotnet/dotnet.exe'
$java = Join-Path $PSScriptRoot 'jdk-21/jdk-21.0.12.1+1'
$sdk = Join-Path $PSScriptRoot 'android-sdk'
$project = Join-Path $workspace 'MetaLol.Standard.Mobile/MetaLol.Standard.Mobile.csproj'
$output = Join-Path $workspace 'artifacts/android-standard/publish'
$signing = Join-Path $PSScriptRoot 'signing/standard-android'
$keystore = Join-Path $signing 'standard.keystore'
$passwordPath = Join-Path $signing 'password.txt'
if ($InstallDependencies) {
    & $dotnet workload install maui-android
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo instalar MAUI Android.' }
    & $dotnet build $project -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=$sdk" "-p:JavaSdkDirectory=$java" -p:AcceptAndroidSDKLicenses=True
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron instalar las dependencias de Android.' }
}
New-Item -ItemType Directory -Path $signing -Force | Out-Null
if (!(Test-Path -LiteralPath $keystore)) {
    if (Test-Path -LiteralPath $passwordPath) { throw 'Existe una contraseña sin su keystore. Revisa la carpeta de firma antes de continuar.' }
    $randomBytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($randomBytes) } finally { $generator.Dispose() }
    $password = [Convert]::ToBase64String($randomBytes)
    [IO.File]::WriteAllText($passwordPath, $password)
    $env:METALOL_ANDROID_KEYPASS = $password
    try {
        & (Join-Path $java 'bin/keytool.exe') -genkeypair -noprompt -storetype PKCS12 -keystore $keystore -alias standard -keyalg RSA -keysize 3072 -validity 10000 -dname 'CN=Meta LoL Standard, OU=Android, O=Meta LoL, C=CL' -storepass:env METALOL_ANDROID_KEYPASS -keypass:env METALOL_ANDROID_KEYPASS
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear la clave de firma.' }
    } finally { Remove-Item Env:METALOL_ANDROID_KEYPASS -ErrorAction SilentlyContinue; $password = $null }
}
if (!(Test-Path -LiteralPath $passwordPath)) { throw 'Falta la contraseña de la firma existente.' }
# A failed signing attempt can leave an aligned but unsigned file whose timestamp
# causes MSBuild to skip signing on retry. Always recreate this generated output.
$intermediateSigned = Join-Path $workspace 'MetaLol.Standard.Mobile/bin/Release/net10.0-android/com.metalol.standard.manual-Signed.apk'
if (Test-Path -LiteralPath $intermediateSigned) { Remove-Item -LiteralPath $intermediateSigned -Force }
# Use an environment reference for both passwords: apksigner consumes a line
# for each password when the same file is passed twice.
$env:METALOL_ANDROID_KEYPASS = [IO.File]::ReadAllText($passwordPath).Trim()
try {
    & $dotnet publish $project -f net10.0-android -c Release -o $output "-p:AndroidSdkDirectory=$sdk" "-p:JavaSdkDirectory=$java" -p:AndroidKeyStore=True "-p:AndroidSigningKeyStore=$keystore" -p:AndroidSigningKeyAlias=standard -p:AndroidSigningKeyPass=env:METALOL_ANDROID_KEYPASS -p:AndroidSigningStorePass=env:METALOL_ANDROID_KEYPASS --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del APK.' }
} finally { Remove-Item Env:METALOL_ANDROID_KEYPASS -ErrorAction SilentlyContinue }
$signed = Get-ChildItem -LiteralPath $output -Filter '*-Signed.apk' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$signed) { throw 'No se encontró el APK firmado.' }
& (Join-Path $java 'bin/java.exe') -jar (Join-Path $sdk 'build-tools/36.0.0/lib/apksigner.jar') verify --verbose $signed.FullName
if ($LASTEXITCODE -ne 0) { throw 'El APK no tiene una firma válida; no se copiará a dist.' }
& (Join-Path $sdk 'build-tools/36.0.0/zipalign.exe') -c -P 16 4 $signed.FullName
if ($LASTEXITCODE -ne 0) { throw 'El APK no tiene una alineación válida.' }
$dist = Join-Path $workspace 'dist/MetaLoL-Standard-Mobile-1.0-android.apk'
Copy-Item -LiteralPath $signed.FullName -Destination $dist -Force
Get-Item -LiteralPath $dist | Select-Object FullName,Length,LastWriteTime
