# Meta LoL Estándar · 1.0

Edición Windows para cualquier jugador y las cinco líneas: Top, Jungla, Mid, ADC y Soporte. Incluye el catálogo completo de campeones de Riot y no contiene una champion pool predefinida, un nick fijo ni resaltados por pertenencia a un pool.

## Instalar y usar

Ejecuta `MetaLoL-Standard-Setup-1.0-win-x64.exe`. El instalador incluye el entorno de ejecución para Windows x64. La primera apertura necesita internet para descargar el catálogo, iconos, objetos y runas desde Data Dragon; después conserva una caché local.

Con el cliente de LoL en selección, la app detecta el rol asignado al jugador local incluso antes de elegir campeón. Muestra el rol y su rival en el matchup y actualiza el ranking cuando cambia la posición. Cuando el cliente no informa el rol, utiliza la selección manual. Si las líneas enemigas no están disponibles, se puede confirmar el rival a partir de los picks detectados.

Los diez selectores de picks muestran el catálogo completo, incluso campeones fuera de su línea habitual. Para las recomendaciones, el motor evalúa todos los candidatos del catálogo de la línea elegida y muestra los cinco disponibles con mejor score. No restringe los resultados a ninguna lista personal. Excluye picks ocupados por otros jugadores y los baneados conocidos.

El ranking considera matchup, estructura aliada, composición rival e historial personal. Los empates se ordenan por WR histórico personal, nivel de maestría, puntos de maestría y nombre. Las tarjetas explican ventajas, riesgos y un plan táctico.

El score sobre 100 es un índice heurístico de encaje, no una probabilidad de victoria ni una predicción entrenada. Las lecturas de kit se distinguen de las muestras estadísticas importadas. En bot, el duelo de una posición no representa por sí solo el 2v2 completo.

## Datos y compatibilidad

El historial y la maestría se recuperan del cliente local para la cuenta conectada. Las partidas se guardan por cuenta e ID sin duplicados y se filtran por rol al calcular el rendimiento. Se consideran partidas PvP de Grieta de al menos cinco minutos con posición identificable. El importador CSV/Riot API conserva el esquema original de matchups de jungla; esas muestras no se atribuyen a otras líneas.

Esta edición tiene ejecutable, identificador de instalación y carpeta de datos propios (`%LOCALAPPDATA%\MetaLol.Standard`). Convive con las ediciones personales sin compartir sus bases de datos. El instalador no incluye historial, credenciales ni datos de otros jugadores.

La interfaz se adapta a 1024×768 y a ventanas menores mediante paneles compactos y desplazamiento vertical. Se puede arrastrar desde fondos y espacios sin texto, conservando la interacción de botones, campos y selectores.

La detección automática depende de la información disponible en la API local del cliente de LoL. No requiere una Riot API key para el draft, el historial propio ni la maestría. Debe comprobarse con el cliente en una selección real tras cada cambio de compatibilidad del juego.

## Desarrollo

```powershell
./tools/dotnet/dotnet.exe test MetaLol.Standard.Tests/MetaLol.Standard.Tests.csproj
./tools/dotnet/dotnet.exe publish MetaLol.Standard/MetaLol.Standard.csproj -p:PublishProfile=win-x64
./tools/inno/ISCC.exe installer/MetaLol.Standard.iss
```

El ejecutable se publica en `artifacts/standard/publish` y el instalador en `dist`. Las pruebas cubren las cinco líneas, el catálogo completo en selectores, top 5 sin pool, estilo uniforme, exclusiones por picks y bans, rol automático antes del pick, cambios de posición, historial por cuenta/rol y vista a 1024×768.
