# Meta LoL

Aplicación de escritorio WPF para Windows que recomienda junglas a partir de la composición rival y aliada.

También incluye `MetaLol.Mobile`, una aplicación Android de estudio con selección manual del draft. La versión móvil comparte el motor y el pool, pero la sincronización con el cliente y el historial personal corresponden a Windows. El APK 1.0 existente no se actualiza con esta entrega.

## Versión 2.13 · Windows

- La ventana se adapta al área útil de la pantalla, incluida la barra de tareas. En un monitor de 1024×768 con área útil de 1024×728 abre a 1008×712, con margen de 8 píxeles.
- Diseño que se reorganiza al redimensionar: paneles compactos por debajo de 1180 unidades WPF y una columna con desplazamiento vertical por debajo de 900. Las tarjetas distribuyen el texto táctico a lo ancho y conservan el tamaño de letra.
- Compatible con el espacio lógico disponible cuando Windows aplica escala de pantalla. El tamaño mínimo principal baja a 640×420 y se limita al área disponible al abrir.
- Arrastre desde márgenes, fondos y huecos sin texto en la ventana principal, detalle e importación. Los botones, selectores, campos, tarjetas pulsables y barras de desplazamiento conservan su interacción. El arrastre requiere mover el mouse, no solo pulsar; un doble clic en el fondo alterna maximizar y restaurar.

## Motor e historial · desde 2.12

- Pool de 15 junglas, incluido Jax.
- Score con dos decimales sobre 100. Es un índice de encaje, no un porcentaje de victoria.
- Con jungla rival y aliados conocidos: matchup directo 45%, resiliencia y estructura aliada 30%, composición rival 15%, historial personal 10%. Sin aliados: 65/0/25/10. Sin jungla confirmado: 0/50/35/15; sin ambos: 0/0/80/20.
- Desempate por score visible, WR histórico personal, nivel de maestría, puntos de maestría y nombre.
- Ventajas y riesgos de kit frente al jungla rival, carencias aliadas, respuesta a la composición enemiga y plan táctico por campeón. Las lecturas de kit se identifican como reglas; no se presentan como estadísticas de victoria.
- Las reglas incorporan presión temprana, duelos, dependencia de ataques básicos, desplazamientos interrumpibles, daño sostenido, anti-tanque y resets. No son pesos entrenados ni una calibración validada de probabilidad de victoria.
- Las muestras importadas de matchup requieren 30 partidas y se regularizan hacia 50%. El historial personal usa un prior de 12 partidas, las últimas 20 por campeón y resultados propios contra ese jungla. La maestría desempata; no suma puntos al score.
- Se muestran también las opciones con score inferior a 51 como `ARRIESGADO`, con sus riesgos. Se excluyen los campeones ya elegidos por otros jugadores y los baneados conocidos.
- Picks de ambos equipos visibles y actualizados cada 2 segundos, con estado provisional y línea por confirmar. Los picks fuera del catálogo de una línea siguen apareciendo. Si el cliente no revela el rol enemigo, puede confirmarse el jungla desde los picks detectados; no se adivina por el orden del equipo.
- Historial y maestría se sincronizan al conectar y cada minuto, con una nueva consulta al cambiar a fin de partida o lobby. Lee hasta 100 entradas recientes disponibles y limita a 12 consultas de detalle por ciclo; completa el resto en ciclos posteriores.
- Guarda partidas personales por cuenta e ID para evitar duplicados. Conserva la caché si un endpoint no responde y separa cuentas por PUUID. Solo considera jungla en partidas PvP de Grieta (colas 400, 420, 430, 440 y 490), de al menos 5 minutos; excluye ARAM, personalizadas y partidas sin posición identificable.
- Las tablas nuevas se añaden sin borrar los matchups existentes. Los agregados importados anteriores no se atribuyen a tu cuenta, porque no contienen esa identidad.

El cliente local debe estar abierto para sincronizar. La disponibilidad de sus endpoints puede variar según el parche. La lectura de un draft real requiere comprobarse con el cliente en selección; las pruebas automatizadas usan sesiones simuladas.

## Estado actual

- Catálogo completo de campeones e iconos desde Data Dragon.
- Sincronización al iniciar y comprobación de nuevo parche cada 30 minutos.
- Roles separados para Top, Jungla, Mid, ADC y Support.
- Recomendaciones automáticas al cambiar cualquier selección; no requiere botón de análisis.
- Lectura automática de los picks y líneas rivales desde la selección de campeón del cliente local de LoL; modo manual disponible cuando no hay draft activo.
- Filtros por región y elo.
- Composición aliada opcional.
- Base SQLite local en `%LOCALAPPDATA%\MetaLol\meta-lol.db`.
- Motor híbrido: reglas explicables y estadísticas reales cuando existe una muestra mínima de 30 partidas.
- Detalle de build adaptativa, objetos y runas del parche al pulsar una recomendación.
- Importación de partidas con Riot API y de agregados mediante CSV.
- Caché offline del catálogo, objetos, runas e iconos.

## Ejecutar en desarrollo

```powershell
dotnet run --project .\MetaLol.Desktop\MetaLol.Desktop.csproj
```

## Pruebas

```powershell
dotnet test .\MetaLol.Tests\MetaLol.Tests.csproj
```

## Publicar para Windows

```powershell
dotnet publish .\MetaLol.Desktop\MetaLol.Desktop.csproj -p:PublishProfile=win-x64
```

El ejecutable autocontenido se genera en `artifacts\publish`. El script de Inno Setup está en `installer\MetaLol.iss` y genera el instalador en `dist`.

## Android

La nueva edición **Estándar para Android** está en `MetaLol.Standard.Mobile`: selección manual de las cinco líneas, catálogo completo sin pool predefinida, búsqueda por nombre y cinco recomendaciones con información táctica. Incluye un catálogo local para funcionar sin internet desde el primer inicio. APK: `dist/MetaLoL-Standard-Mobile-1.0-android.apk`. [Uso y compilación](MetaLol.Standard.Mobile/README.md).

La descripción siguiente corresponde al APK móvil anterior, que se conserva como edición independiente.

La aplicación móvil permite elegir manualmente los cinco rivales y los cuatro aliados. Las recomendaciones se actualizan al instante, están limitadas al champion pool configurado y no se conectan al cliente de League of Legends.

El APK ARM64 instalable se genera en `dist\MetaLoL-Mobile-1.0-android-arm64.apk`. Para instalarlo fuera de Google Play, copia el archivo al teléfono y autoriza temporalmente la instalación de aplicaciones desconocidas para el gestor de archivos usado.

La primera apertura necesita internet para descargar el catálogo actual de Data Dragon. Después conserva una copia local para funcionar sin conexión.

## Importar estadísticas reales

En la aplicación, pulsa `IMPORT DATA`. La clave de Riot se usa únicamente durante esa importación y no se guarda. También puedes importar un CSV con este esquema:

```text
patch,region,tier,jungler_id,opponent_id,games,wins
16.20,LAS,ALL,Vi,LeeSin,500,270
```

Los IDs deben coincidir con Data Dragon, por ejemplo `MonkeyKing`, `LeeSin` o `JarvanIV`. Hay una plantilla vacía en `samples\counters-template.csv`.

## Datos y seguridad

- No se copian ni redistribuyen estadísticas propietarias de MetaSRC.
- Los catálogos visuales provienen de Data Dragon.
- La API key de Riot no se incrusta en el ejecutable ni se persiste.
- La conexión con el cliente de LoL usa su lockfile únicamente en memoria y consulta su API HTTPS local; no envía credenciales ni picks a servidores externos.
- Sin una muestra importada, la interfaz muestra explícitamente `RULE ENGINE` y `Sin muestra estadística`.
