# Meta LoL · Nÿf ú #LAS · Edición 1.0

Edición Windows independiente para Nÿf ú #LAS, con Top, Jungla, Mid, ADC y Soporte. Conserva el diseño que se adapta a 1024×768 y el arrastre desde zonas vacías.

## Instalación y uso

Comparte `dist/MetaLoL-Nyfu-LAS-Setup-1.0-win-x64.exe` con tu amigo. Su nick aparece en el título, el asistente de instalación y el acceso directo. Esta edición usa su propio ejecutable, identificador de instalación y carpeta de datos `%LOCALAPPDATA%\MetaLol.Nyfu`; puede convivir con Meta LoL 2.13.

La primera apertura necesita internet para descargar el catálogo oficial de Riot, iconos, objetos y runas. Después utiliza la caché disponible. No necesita una Riot API key para la detección del draft, historial propio o maestría desde el cliente local.

Fuera del draft, selecciona una línea y los picks rivales y aliados. Los diez selectores de picks muestran todos los campeones del catálogo, incluidos picks fuera de su línea habitual. El motor reserva tu posición y evalúa el catálogo de candidatos de esa línea, sin limitarlo al pool personal. Muestra como máximo las cinco opciones disponibles con mejor score; descarta campeones elegidos por otros jugadores y los baneados conocidos.

Con el cliente en selección, la app lee la posición asignada a la celda del jugador local incluso antes de elegir campeón. Muestra `TU LÍNEA · AUTOMÁTICA · LCU` y el rival de esa posición en el matchup. Un cambio de rol actualiza las recomendaciones aunque los picks no cambien. Cuando el cliente no revela una posición, conserva el rol manual sin adivinarlo. Si no revela las líneas enemigas, se puede confirmar el rival desde los picks detectados. Las selecciones provisionales se identifican como tales.

## Pool resaltado en verde

Ahri, Kayn, Amumu, Darius, Diana, Caitlyn, Xayah, Lucian, Miss Fortune, Vayne, LeBlanc y Ekko. Se unificaron ambas listas, corrigiendo `arhi` y `Catilyn` y contando Diana una sola vez.

El pool se muestra con borde, fondo, nombre y etiqueta verdes cuando aparece entre los cinco primeros. También se distingue en las listas de picks. Ser del pool no suma un bonus artificial ni fuerza un lugar en el top 5. Los empates se ordenan por WR personal, nivel de maestría, puntos de maestría y nombre.

## Cálculo e historial

El score es un índice heurístico de encaje sobre 100 con dos decimales, no una probabilidad de victoria ni una predicción entrenada. Considera el matchup de la línea, la estructura aliada, las amenazas enemigas y el historial personal. Conserva los pesos base de Meta LoL: 45/30/15/10 cuando se conocen rival y aliados, redistribuidos cuando faltan datos. Incluye lecturas de kit y planes para los campeones del pool. En bot también considera presión temprana, control y protección aliada; se advierte que un duelo ADC contra ADC o soporte contra soporte no representa por sí solo el 2v2.

El historial se guarda por cuenta e ID de partida, y se filtra por rol para calcular el WR. Solo admite partidas PvP de Grieta de al menos cinco minutos y posiciones identificables. El importador de agregados conserva el esquema original de jungla; esas muestras no se utilizan como estadísticas de mid, top, ADC o soporte. El historial propio de las cinco líneas y la maestría se recuperan automáticamente cuando el cliente los ofrece.

La app consulta únicamente la API HTTPS local del cliente con sus credenciales en memoria y el contenido público de Data Dragon. El instalador no incluye claves, historial personal ni datos de la edición principal. La detección depende de la información que exponga el cliente en cada parche y debe verificarse con el cliente de Nÿfu en una selección real.

## Desarrollo

```powershell
./tools/dotnet/dotnet.exe test MetaLol.Nyfu.Tests/MetaLol.Nyfu.Tests.csproj
./tools/dotnet/dotnet.exe publish MetaLol.Nyfu/MetaLol.Nyfu.csproj -p:PublishProfile=win-x64
./tools/inno/ISCC.exe installer/MetaLol.Nyfu.iss
```

El ejecutable se genera en `artifacts/nyfu/publish`. Las pruebas incluyen las cinco líneas, rol antes del pick, cambio de rol sin cambio de campeones, catálogo completo en selectores, top 5, colores del pool, exclusión de picks y bans, historial por rol/cuenta y vista a 1024×768.
