# CLAUDE.md – app_restatify-tensegrity-ai

Uno-Platform-App (Skia, MVUX, Navigation-Extensions, Material). Fachliche Referenz: Docs-Repo (ADR-0005/9).

## Regeln
- Inferenz hinter einer Schnittstelle mit zwei Implementierungen: Core-FFI (nativ) und Cloud (WASM).
- WASM: „Streng geheim“ und „Nur lokal“ nicht wählbar, mit Erklärung.
- Stufenwahl gut sichtbar; Taint (strengste Stufe gewinnt) in der UI nachvollziehbar.
- Keine Prompts/Antworten loggen; keine Telemetrie bei „Nur lokal“.
- Mobil-Performance: UI-Thread frei halten, Streaming-Antworten, Modell nur bei Bedarf laden.
- Texte über Lokalisierung (`x:Uid`), Deutsch mit echten Umlauten.
- Tests: MSTest, Playwright (WASM), Uno.UITest/Appium (mobil).
- Kein `git push` ohne Freigabe.
