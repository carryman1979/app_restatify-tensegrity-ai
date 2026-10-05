# Sicherheitsrichtlinie

## Schwachstellen melden

Bitte **keine öffentlichen Issues** für Sicherheitslücken. Nutze stattdessen
**GitHub Private Vulnerability Reporting** im betroffenen Repository (Reiter „Security“ → „Report a vulnerability“).

Besonders relevant sind:

- Abfluss von Daten der Stufen „Streng geheim“ oder „Nur lokal“,
- Umgehung von Signaturprüfungen bei Releases und Delta-Paketen,
- Vergiftung des Hive (Umgehung von Trust, Clipping oder Eval-Gate),
- Fehler im Privacy Accountant oder in der DP-Kalibrierung.

## Ablauf

1. Eingangsbestätigung angestrebt innerhalb von 7 Tagen.
2. Bewertung und Fix in einem privaten Fork.
3. Koordinierte Veröffentlichung mit Advisory; Nennung der Meldenden auf Wunsch.

## Unterstützte Versionen

Bis zur Version 1.0 wird nur der jeweils neueste Stand von `main` unterstützt.
