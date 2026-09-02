# Crystal Sprint

Stilisierte First-Person-Waldszene mit vollständiger Weltfigur für Unity 6000.5.10f1 / URP 17.5.
Hauptszene: `Assets/Scenes/CrystalSprint.unity`.

## Steuerung

- WASD / linker Stick: Bewegen; Shift: Sprinten.
- Maus / rechter Stick: Kamera; Leertaste / Gamepad South: Springen.
- 1–4: Inventarslot; Slot 1: Carpentry-Axt; linke Maustaste: ein Axtschlag.
- Esc: Cursor freigeben; Klick ins Spiel: erneut sperren.
- R / Start: Neustart.

## Aktueller Wald-Umbau

Die Terrainfläche ist 135 × 135 statt 96 × 96 Meter groß (1,98-fache Fläche).
Die spielbare Waldgrenze liegt bei etwa 58,69 statt 41,5 Metern Radius.
Der ursprüngliche 65 × 65 Terrain-Kern einschließlich Teichmulde blieb vertexgenau
erhalten. Der äußere Berghang ist erweitert und bleibt durch seinen steilen
Außenbereich eine kollidierende Begrenzung. Felsformationen sind für alle LODs
neu auf dem Hang aufgesetzt.

Vegetation stammt aus dem bereits importierten **Vegetation Stylized Pack by
LUX ART STUDIOS**:

- 240 Bäume: `S_Tree_A` bis `S_Tree_J`, mit Größen-/Rotationsvariation und LODs.
- 140 Büsche: `S_Bush_A` bis `S_Bush_D`, ohne störende Gameplay-Collider.
- Rund 26.000 dichte Grasbüschel: `S_Grass_01A`, `S_Grass_02A`, `S_Grass_C`.
- Breitere geschwungene Korridore, Teichzugang und Startlichtung bleiben offen.
- 13 springbare Stümpfe und die bisherigen Nature-Details bleiben erhalten.
  Die Erweiterung ergänzt 6 Stämme, 60 Pilzgruppen, 40 Äste und 20 Steine.

Eigene URP-Materialkopien liegen unter `Assets/Materials/ForestKit`, verknüpfte
Vegetations-Prefabs unter `Assets/Prefabs/ForestKit`. Originaldateien der Packs
wurden nicht verändert. Der Lizenzhinweis **LUX ART STUDIOS** ist im HUD sichtbar;
maßgeblich bleibt die mitgelieferte Lizenz im importierten Pack.

## Gras und Performance

`InstancedForestGrass` rendert die originalen Kit-Geometrien in kleinen räumlichen
GPU-Instancing-Gruppen statt einzelner Gras-GameObjects. Die Root-Space-Meshkopien
bewahren Form und UVs der Importmodelle. Frustum-Culling, Distanz-Ausdünnung und
weiches Ausblenden bis 65 Meter begrenzen den Aufwand.

`KitInteractiveGrass.shader` übernimmt Wind und den bestehenden
`_GrassInteractor`. Nur der bodennahe, auf Gras stehende Spieler biegt die Spitzen;
beim Springen oder auf Stümpfen wird die Grasinteraktion deaktiviert. Die Wurzeln
werden auf den tatsächlich triangulierten Boden gesetzt. Das bestehende
`InteractiveGrass`-Steuerscript bleibt unverändert.

## Axt und Animation

Das bestehende Carpentry-Modell `Axe_Straight`, seine Materialien, der Icon-GUID
und das Vier-Slot-Inventar bleiben erhalten. Zwei-Bone-Arme mit Ellenbogen und
Handgelenk führen den Griff exakt durch die Hand; der Schaft steht etwa 90 Grad
zum Unterarm, auch in der gespeicherten Prefab-Referenzpose.

Es gibt eine einzige 1,15-Sekunden-Schlagbewegung: Vorbereitung, kontrollierter
Abwärtsschlag, Nachschwingen und Rückkehr. Oberkörperrotation, Gewichtsverlagerung
und Gegenbewegung des freien Arms ergänzen die Gelenkbewegung. Gesicht, Hut,
Bart und Kleidung wurden nicht neu modelliert.

## Licht und Himmel

`ForestURP` ist eine eigene Kopie der zuvor funktionierenden Pipeline-Konfiguration.
Seitliches warmes Sonnenlicht, weiche 4-Kaskaden-Schatten (90 Meter), schwaches
Himmels-Fülllicht, abgestimmtes Ambient Light, Fog und ACES/Color Grading sorgen
für deutliche, aber lesbare Waldschatten.

Der importierte `Skybox/Cubemap Extended` wurde unter URP im A/B-Vergleich
gerendert. Seine Demo-Cubemap zeigte stärkere Farb-Abstufungen und keine zur
Hauptlichtquelle passende Sonne. Aktiv bleibt deshalb eine abgestimmte Kopie des
bisherigen prozeduralen Himmels. Die getestete Extended-Materialkopie bleibt
als Alternative im ForestKit-Materialordner verfügbar.

## Erhaltene Systeme und Prüfung

Bewegung, Sprint, Kamera, Cursorsteuerung, Springen, Inventar, Neustart,
Wasserbewegung, Fischsprünge und Wassereffekte bleiben erhalten.
Der alte `Generate Game`-Generator überschreibt die integrierte Szene nicht.
Die bisherige Nature-Integrationsprüfung erkennt die neue Vegetationsintegration.

Unity-Play-Mode-Prüfung am 02.09.2026: **15 bestanden, 0 fehlgeschlagen**.
Sie prüft außerdem alle 13 Stumpflandungen, Materialherkunft, 10 Baum-/4 Busch-
Varianten, Terrain-Erhalt, Bergkollision, Grashöhen, Sprung-Bending-Gating,
Axt-Griffwinkel und Körperfreiheit während Gehen, Sprinten und wiederholten Schlägen.

- Testergebnis: `Logs/dense-forest-tests-verified.xml`.
- Gerenderte Mehrwinkel-/Animations-/Skybox-Ansichten: `Logs/DenseForestReview/`.
- Renderbenchmark: `Logs/DenseForestReview/performance.txt`; synchronisierte
  Renderzeit inklusive GPU-Readback, kein pauschaler Standalone-FPS-Wert.
- Vorheriger Stand: neben dem Projekt in
  `Crystal-Sprint-main-backups/before-dense-forest-20260902`.

Editor-Werkzeuge liegen unter `Tools > Crystal Sprint`.
`Apply Dense Vegetation Forest Upgrade` ist ein bewusster Neuaufbau der
Vegetationsplatzierung und sollte nicht auf ungespeicherte manuelle Szenenänderungen
angewendet werden. `Validate Dense Forest` prüft nur den vorhandenen Stand.

## First-Person-Perspektive

Die Hauptszene startet jetzt direkt in First Person. `FirstPersonCamera` sitzt
etwa 1,78 Meter über den Füßen, folgt ohne Positionsglättung und übernimmt die
Blick-/Körperrichtung. Seitwärts- und Rückwärtslaufen drehen den Körper nicht von
der Blickrichtung weg. Maus-Sensitivität: 0,10 Grad pro Pixel; Blickbegrenzung:
80 Grad nach oben / 75 Grad nach unten. Die bestehenden Cursor-, WASD-, Sprint-,
Sprung-, Inventar- und Neustart-Bindings bleiben erhalten.

`Assets/Prefabs/FirstPerson/LumberjackArms.prefab` enthält separate runde Ärmel,
Hände und die verschachtelte, bisherige Carpentry-Axt. Eine URP-Overlay-Kamera
zeichnet nur den Layer `FirstPersonViewmodel`, mit eigenem Tiefenpuffer und
70 Grad Sichtfeld. Sie verhindert, dass nahe Umgebung die Arme/Axt abschneidet;
zusätzlich zieht sich die Haltung vor Hindernissen leicht zurück. Die bisherige
Farbkorrektur wird einmal auf den vollständigen Kamerastapel angewendet.
Eigene Materialkopien unter `Assets/Materials/FirstPerson` verwenden die
bisherigen Farben und Originaltexturen; keine importierten Materialien wurden geändert.

Das Viewmodel folgt dem vorhandenen 1,15-Sekunden-Schlag und dem ausgerüsteten
Inventarslot. Zwei-Gelenk-Arme halten den Griff exakt in der Hand. Gehen/Sprinten
ergänzen dezente Bewegungen der Arme, aber kein verzögertes Kamerawackeln.
`LumberjackEquipment`, `LumberjackVisual`, das Spieler-Prefab und die Welt-Axt
bleiben unverändert. Die World-Figur bleibt aktiv: `FirstPersonBodyVisibility`
setzt sie nur während des Renderns der eigenen Kamera auf reine Schatten und
stellt ihre Darstellung für andere Kameras/Spiegelungen wieder her. Kopf,
Bart und Hut wurden nicht gelöscht. Der bisherige `ThirdPersonCamera`-Controller
bleibt als deaktivierte Komponente vorhanden; es gibt keinen neuen Umschaltknopf.

Map, Vegetation, Terrain, Licht und UI wurden nicht umgebaut. Das Gras überspringt
lediglich Kameras, deren Layer-Maske kein Gras rendert, damit die zusätzliche
Arme-Kamera keine unnötigen Gras-Draw-Calls einreicht.

Prüfungen und echte gerenderte Play-Mode-Ansichten:

- Unity-Prüfung vom 02.09.2026: **22 bestanden, 0 fehlgeschlagen**, keine
  Compilerfehler. Einschließlich echter Input-System-Maus-/Tastaturereignisse,
  Esc/Klick, 101 Axt-Posen mit Griff-/Gelenk-/Kameraabstandsprüfung sowie aller
  15 bisherigen Gameplay-/Environment-Tests.
- `Logs/first-person-tests.xml`: komplette Play-Mode-Test-Suite.
- `Logs/FirstPersonReview/`: Idle, Blickgrenzen, vier Schlagphasen, Bewegung,
  Sprint, Sprung, nahes Hindernis und erhaltene Weltfigur.
- `Logs/FirstPersonReview/installation.txt`: Vergleich der unveränderten
  Szenenobjekte außerhalb von Spieler/Kamera.
- Editor: `Tools > Crystal Sprint > Install First Person`; Installation ist
  wiederholbar und verweigert ungespeicherte Szenen.
- Sicherung vorher: `Crystal-Sprint-main-backups/before-first-person-20260902`
  neben dem Projekt.

## Hütte am Teich und neuer Wasser-Shader

Die vollständige `CozyCabin`-Zusammenstellung aus
`Assets/Cozy Mountain Cabin/Demo/Sample.unity` ist als eigenes, verschachteltes
Prefab `Assets/Prefabs/PondCabin/PondsideCabin.prefab` integriert. Die importierten
Originale bleiben unverändert. Standort: östlich des Teichs, ungefähr
`(18, 0.060, 4)`, Y-Rotation 180 Grad, einheitliche Skalierung **1,35**.
Der Eingang zeigt nach Süden auf trockenen Boden, nicht zum Wasser.

Die Türöffnung ist ca. 1,49 m breit und 2,84 m hoch. Die Tür startet geschlossen
und lässt sich mit E weich um 95 Grad öffnen und wieder schließen.
Die einseitigen Wand-/Türrahmen-MeshCollider der Demo sind deaktiviert und
durch solide Kollisionskörper ersetzt (Details unten). Ergänzt wurden ein
durchgehender Innenboden aus Holzdielen und ein niedriges Stein-Fundament.
Wände und Vorhänge werden auch von innen dargestellt. Eigene URP/Lit-
Materialkopien verwenden die mitgelieferten Textur-/Normal-/Metallic-Atlanten;
eine dezente lokale Innenbeleuchtung ergänzt die vorhandene Außenlampe.

15 störende Pflanzen/Objekte wurden im angrenzenden Wald versetzt, nicht gelöscht.
240 Bäume, 140 Büsche und 13 Stümpfe bleiben erhalten. Nur Gebäude, Veranda,
Zugang und die neuen Standorte größerer Objekte erhalten Grasfreistellungen.
Alle übrigen Grasinstanzen behalten ihre bisherigen Transformationen.

Das Teichmaterial `Assets/Materials/PondCabin/Pond_SimpleWater.mat` verwendet
den importierten Shader `Custom/SimpleWaterURP` aus **Houidisoft technology /
Simple water**, dessen originale Normalmap und eine kleine erzeugte Schaum-Maske.
Aktiv sind bewegte Oberflächennormalen, Tiefenfarben/-transparenz,
Fresnel-Reflexion und dezenter Uferschaum. Die Kamera liefert die nötige
Depth Texture. Zusätzliche GPU-Höhenwellen sind ausgeschaltet, damit die
bestehenden CPU-Wellen, Fischkontakte und Ringwellen dieselbe Wasserhöhe nutzen.

Der alte radiale Teichboden überschnitt sich zwischen seinen Stützpunkten mit
dem Terrain. Nur seine sichtbare Bodenschicht wurde durch ein passendes Mesh
aus der unveränderten Terrain-Triangulierung ersetzt, 12 mm darüber und ohne
eigenen Collider. Eine hellere Kopie des bisherigen Bodenmaterials vermeidet
schwarze Uferflächen; grüne Durchdringungs-Dreiecke verschwinden dadurch.

Das Paket enthält einen Wasser-Shader, keine Splash-/Ripple-Prefabs. Deshalb
bleiben `WaterSplash`, `WaterRipple`, alle drei Fischmodelle und der
10-Sekunden-Takt erhalten. `PlayerWaterInteraction` nutzt kleinere Varianten
derselben Effekte beim Ein-/Austritt und beim Waten. Austrittseffekte bleiben
am letzten Wasserkontakt, nicht auf trockenem Land. Gras-Bending und Staub
pausieren bei Wasserkontakt; Bewegung und Kollisionen bleiben unverändert.

Unity 6000.5.10f1, Prüfung am 02.09.2026: **29 Play-Mode-Tests bestanden,
0 fehlgeschlagen**, keine Compilerfehler. Enthalten sind echter
Controller-Durchgang durch die Tür in beide Richtungen, Boden-/Wandkollision,
trockener Zugang, Shaderprüfung, Wasserinteraktion und alle bisherigen
First-Person-/Inventar-/Axt-/Fisch-/Environment-Tests.

- Ergebnisse: `Logs/cabin-water-tests.xml`.
- Echte gerenderte Play-Mode-Ansichten: `Logs/CabinWaterReview/`, darunter
  Hüttenansichten, Innenraum, Shader-A/B-Vergleich und Fisch-/Spielereffekte.
- Sicherung vorher: `Crystal-Sprint-main-backups/before-cabin-water-20260902`
  neben dem Projekt. Keine bisherigen Dateien wurden gelöscht.
- `Tools > Crystal Sprint > Integrate Pond Cabin And Water` ist eine einmalige
  Integration und verweigert eine bereits vorhandene Hütte, um manuelle
  Szenenänderungen zu schützen. Für Änderungen das neue Hütten-Prefab bearbeiten.

## Beidseitige Hütten-Kollision und allgemeines Benutzen

Die Ursache des Innen-nach-außen-Bugs waren offene, einseitige Wandflächen im
nicht-konvexen MeshCollider. Sichtbare Rückseiten durch ein doppelseitiges Material
machen den Physik-Collider nicht doppelseitig. Außerdem fehlte beim vorderen
`SmallWindow` ein Collider. Trigger, Layer-Matrix und CharacterController waren
nicht die Ursache; diese globalen Einstellungen wurden nicht verändert.

`Solid Building Colliders` im Hütten-Prefab enthält passgenaue BoxCollider für
Wandabschnitte, Fensterbrüstungen und Türrahmen. Die Türöffnung bleibt ausgespart.
Beide seitlichen Fenster behalten ihre funktionierenden BoxCollider; das kleine
Fenster erhielt einen passenden BoxCollider. Zwei geschlossene konvexe Prismen
decken die dreieckigen Giebel ab. Alte einseitige Wand-/Rahmen-Collider sind
deaktiviert, nicht aus den importierten Assets gelöscht.

Die Regression wurde zuerst reproduziert: `cabin-collision-before.xml` zeigt
die Fehler von innen an Wänden und Frontfenster. Danach bestanden alle sechs
Richtungstests in `cabin-collision-fixed.xml`, **bevor** die Interaktionen
implementiert wurden: 110 Wand-/Fensterproben mit dem echten CharacterController
auf Lauf-/Sprunghöhe sowie Durchgang durch die Tür in beide Richtungen.

`IInteractable` definiert die gemeinsame Benutzen-Schnittstelle.
`PlayerInteractor` wählt über die First-Person-Kamera genau ein anvisiertes Ziel
innerhalb von 2,75 m. Feste Collider verdecken Ziele dahinter; fremde Trigger und
der eigene Spieler werden übersprungen. E wird zentral über `GameInput` nur bei
neuem Tastendruck ausgewertet (alternativ Gamepad-West-Taste). Bei freiem Cursor
findet keine Interaktion statt. Der kontextsensitive Hinweis lautet allgemein
`E – Benutzen`. Weitere Objekte implementieren dieselbe Schnittstelle und
benötigen einen Collider bzw. einen passenden Auswahl-Trigger.

`HingedDoorInteractable` animiert das geschlossene Türblatt in 0,8 Sekunden auf
95 Grad, mit weichem An-/Auslauf. Der Modell-Pivot wurde auf die äußere
Scharnierkante gesetzt; eine eigene Meshkopie hält das geschlossene Türblatt
dabei exakt an seiner bisherigen sichtbaren Position. Ein kinematischer
Rigidbody bewegt den vorhandenen soliden Tür-BoxCollider. Der Schwenk pausiert,
wenn der Spieler im Weg steht, und setzt sich fort, sobald er Platz macht.
Eine blockierte Bewegung kann mit E umgekehrt werden.

`CurtainInteractable` verwendet vier Vorhangpaare: drei Fensterpaare und das
vorhandene Türvorhang-Element, das anfangs seitlich geöffnet ist. Die originale
Stoffgeometrie wurde entlang der Mitte in zwei wiederverwendbare Meshkopien
geteilt, mit erhaltenen UVs, Falten und Materialien. Beide Hälften werden weich
zu den Außenkanten gerafft; Höhe und Faltentiefe bleiben konstant. Nur der
sichtbare Stoff hat nicht-blockierende Auswahl-Trigger. Ein offener Türvorhang
verdeckt deshalb die Tür nicht mit einem unsichtbaren Interaktionsbereich.

- Sicherung: `Crystal-Sprint-main-backups/before-cabin-collisions-interactions-20260902`
  neben dem Projekt.
- Tests: `Logs/cabin-final-tests.xml`; zusätzliche echte E-Ereignisse,
  Einzelauswahl, Reichweite/Verdeckung, geschlossene Tür von beiden Seiten,
  Türschwenk gegen feste Bauteile, Spielerschutz und alle Vorhangpaare.
- Abschließende Unity-Prüfung am 02.09.2026: **40 bestanden, 0 fehlgeschlagen**,
  keine Compilerfehler. Die sechs beidseitigen Kollisions-/Durchgangstests wurden
  nach allen Interaktionsänderungen nochmals erfolgreich ausgeführt.
- Gerenderte Animationsphasen: `Logs/CabinInteractions/`.
- Spielerbewegung, First-Person-Arme/Axt, Inventar, Wasser/Fische, Terrain,
  Vegetation und deren Materialien wurden nicht verändert.
