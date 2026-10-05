# Mitwirken an Tensegrity AI

Danke für dein Interesse! Diese Regeln gelten für **alle** Tensegrity-AI-Repositories.

## Grundsätze

- **Datensouveränität zuerst.** Änderungen dürfen die Garantien der Vertraulichkeitsstufen (Whitepaper Abschnitt 2 und 5)
  nicht abschwächen. „Nur lokal“ sendet 0 Byte – immer.
- **Contracts zuerst.** Schnittstellen werden in `proto_restatify-tensegrity-ai` geändert, bevor Implementierungen folgen.
- **Nachvollziehbarkeit.** Jede fachliche Änderung verweist auf eine Anforderung (`REQ-…`), ein ADR oder ein Issue.
- **Tests nach V-Modell.** Unit-, Komponenten-, Integrations- und Systemtests – siehe
  [Teststrategie](https://github.com/carryman1979/docs_restatify-tensegrity-ai/blob/main/testing/teststrategie.md).

## Ablauf

1. Issue anlegen oder ein vorhandenes übernehmen.
2. Branch von `main`: `feature/<kurz>`, `fix/<kurz>` oder `docs/<kurz>`.
3. Commits nach [Conventional Commits](https://www.conventionalcommits.org/de/) (`feat:`, `fix:`, `docs:`, …).
4. Pull Request mit ausgefüllter Vorlage; alle Prüfungen grün.
5. Mindestens ein Review; sicherheitsrelevante Änderungen zusätzlich durch Security-Review.

## Developer Certificate of Origin

Commits werden mit `git commit -s` signiert (DCO, <https://developercertificate.org/>). Damit bestätigst du, dass du
den Beitrag unter der Apache License 2.0 einbringen darfst.

## Abhängigkeiten und Modelle

Nur Abhängigkeiten und Modellgewichte mit Apache-2.0-kompatibler Lizenz (z. B. Apache-2.0, MIT, BSD). Keine Daten aus
Diensten, deren Nutzungsbedingungen das Training anderer Modelle verbieten.

## Sprache

Dokumentation auf Deutsch mit echten Umlauten; Code, Bezeichner und Commit-Typen auf Englisch.
