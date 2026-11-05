# Meta LoL Estándar · Android manual

Edición independiente, identificador `com.metalol.standard.manual`. Reutiliza el motor de `MetaLol.Standard`, sin pool ni cuenta predefinida. No accede al cliente de LoL ni al historial de Windows.

- Selección manual entre Top, Jungla, Mid, ADC y Soporte.
- Búsqueda de todo el catálogo para cada posición de ambos equipos, incluidos picks fuera de rol. Evita duplicados; deja libre la posición propia.
- Al cambiar de rol, identifica al rival de esa posición y elimina el aliado de la nueva posición propia.
- Hasta cinco recomendaciones por score para el rol elegido, con motivos, matchup, riesgos, plan y desglose. Los campeones ocupados quedan excluidos.
- Sin partidas o maestría importadas, esos datos no favorecen ningún campeón. Los empates completos terminan por nombre; el score no representa probabilidad de victoria.
- Catálogo de Data Dragon incluido en el APK y caché local de actualizaciones. La primera apertura funciona sin red; internet permite actualizar el catálogo y mostrar retratos.
- Interfaz desplazable, botones táctiles y selector con búsqueda; respeta las barras del sistema y el teclado.

## Instalar

Copiar `dist/MetaLoL-Standard-Mobile-1.0-android.apk` al teléfono, abrirlo y permitir la instalación desde el gestor de archivos si Android lo solicita. El APK incluye ARM64 y ARM de 32 bits. No requiere Python ni un PC para funcionar.

## Compilar

Con .NET 10, MAUI Android, JDK 21 y el SDK Android instalados localmente:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Build-StandardAndroid.ps1
```

Agregar `-InstallDependencies` para preparar las dependencias con descarga de internet. El script crea una firma propia en `tools/signing/standard-android`, fuera de los recursos empaquetados. Conservar esa carpeta de forma privada para futuras actualizaciones; no distribuir el keystore ni su contraseña.

Proceso de publicación basado en la [documentación oficial de Microsoft](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0).

## Pruebas

```powershell
tools/dotnet/dotnet.exe test MetaLol.Standard.Mobile.Tests/MetaLol.Standard.Mobile.Tests.csproj
```

Las pruebas verifican el draft manual, búsqueda, selección del oponente, cambios de línea y ranking en los cinco roles con el catálogo incluido. No sustituyen una prueba en un teléfono físico.
