# Elians Eimer-Bewegungsprobe

Die Szene zeigt Elian vor einem ruhigen Hintergrund: zum Henkel greifen, den Eimer aufheben, zum Trog tragen, mit der zweiten Hand unterstützen, anheben, kippen, ausgießen und zur Tragehaltung zurückkehren.

## In Unity ansehen

1. Menü **Arkmon > Animationsprobe > Eimer öffnen** wählen (oder `Assets/Scenes/AnimationStudies/ElianBucketMotionStudy.unity` öffnen).
2. **Play** drücken. Die Probe startet automatisch.
3. Mit **Pause**, **Neustart**, **Richtung**, **Tempo** und der Zeitleiste den Ablauf untersuchen.

Tastatur: **Leertaste** pausiert; **Pfeil links/rechts** schaltet in Schritten von 1/24 Sekunde; **F** spiegelt die Richtung; **R** startet neu. Das Tempo lässt sich zwischen normal und 0,25× umschalten. Die Zeitleiste kann vorwärts und rückwärts bewegt werden.

Der volle Eimer ist die Ausgangslage dieser isolierten Probe. Der Brunnen und die Auftragslogik gehören nicht zu dieser Szene. Die gesamte Folge dauert 14 Sekunden. Der Neustart setzt die Ausgangslage zurück; er ist ein sichtbarer Szenenreset und keine zusätzliche Abstellanimation.

## Aufbau

`ElianBucketMotionStudy` verwendet dieselben Körperteile und dieselbe feste Armsteuerung wie der Hof. Die Bewegungsabfolge ist separat und lässt sich über `Evaluate(time)` unabhängig von der Bildrate abtasten. Die tiefere Hocke der Probe erreicht den Henkel vor dem ersten Anheben. Die Hand hält den Eimer während des Tragens; beim Ausgießen kommt die zweite Hand an den unteren Eimerrand. Die Fließphase beginnt erst nach dem Kippen.

Die schlichte Bühne und der Trog sind aus Geometrie aufgebaut. Originalzeichnungen werden nicht verändert. Die Probe besitzt keine Spielaufträge, speichert keinen Fortschritt und steht nicht in den Build-Szenen. Die Hof-Aufgaben bleiben bei ihrer bisherigen Ablaufsteuerung, bis die Referenzbewegung beurteilt wurde.

## Prüfung und Aufnahmen

`ElianBucketMotionStudyTests.ReviewBothDirectionsAndExportMotion` nimmt dieselbe Szene in beiden Richtungen auf. Geprüft werden der Übergang am Henkel, die Verbindung von Hand und Eimer, aufeinanderfolgende Positionen sowie reproduzierbares Vorwärts-/Rückwärtsspulen. Die gerenderten Abläufe müssen zusätzlich visuell beurteilt werden.

Ohne `ARKMON_STUDY_FULL=1` entstehen 15 Schlüsselbilder pro Richtung. Mit dieser Variable werden je 337 Bilder bei 24 Bildern pro Sekunde aufgenommen. `ARKMON_STUDY_OUTPUT` bestimmt den Ausgabeordner (Standard: `Library/ElianBucketStudy`).

Version bleibt 0.51.1; für den nächsten tatsächlichen Commit ist 0.51.2 vorgeschlagen. Es wurde kein Windows-Build und kein Commit erstellt.

Vorgeschlagener Commit: `feat(animation): add isolated Elian bucket motion study` — separate Referenzszene mit wiederholbarem Ablauf, Zeitlupe, Richtungswechsel, Zeitleiste und Bildfolgen zur Sichtprüfung.

## Vorschauvideos

- [Normaltempo: rechts, danach links](Elian-Eimer-Normaltempo.mp4), 28,08 Sekunden.
- [Halbes Tempo: rechts, danach links](Elian-Eimer-Zeitlupe.mp4), 56,17 Sekunden.
- [Übersicht der Schlüsselposen](Schluesselposen.jpg).
- `INFO.json` enthält Bildrate, Szenenpfad und Video-Prüfsummen.

Die finalen Bilder liegen unter `Library/ElianBucketStudyGrounded`: je 337 Bilder bei 24 fps. Der Export lässt sich vom Projektordner aus mit `tools/export_elian_bucket_study.ps1` wiederholen; FFmpeg muss im Suchpfad liegen oder über `-Ffmpeg` angegeben werden. Die Aufnahme erfolgt mit dem PlayMode-Filter `ElianBucketMotionStudyTests`, `ARKMON_STUDY_FULL=1` und `ARKMON_STUDY_OUTPUT` auf diesen Ordner.

Die überarbeitete Hebepose führt den Ellenbogen unter dem Gesicht vorbei. Der Stützpunkt wandert von der näheren Unterkante zum gekippten Eimerboden. Beide Kontakte werden in jedem vollständig unterstützten Bild geprüft. Ein kleiner Schwung begleitet den freien Arm beim Tragen. In dieser Probe werden die sichtbaren Stiefelumrisse am Boden ausgerichtet; dazu werden nur vorhandene Alphawerte gelesen, die Zeichnungen bleiben unverändert.

Abschlussprüfung: `Library/elian-bucket-study-reviewed.xml` bestand mit 3/3 Prüfungen einschließlich der beiden vorhandenen Hof-Eimerprüfungen. Nach der abschließenden Fußplatzierung und Beschriftung bestand `Library/elian-bucket-study-grounded.xml` die gesamte Probe erneut. Die beiden Videos wurden vollständig ohne Decodierfehler geprüft. Die nächste Beurteilung betrifft die sichtbare Bewegung in Unity oder im Clip.
