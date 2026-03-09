# Projekt Dokumentation der Disruption Management System mit OPC UA Variablenintegration

## Einleitung
Dieses Dokument beschreibt das gesamte Disruption Management System, das mit OPC UA Variablen integriert ist. Diese Dokumentation ist für Entwickler und Ingenieure gedacht, die das Projekt nachbauen oder weiterentwickeln möchten.

## Technische Details
- **Programmiersprache:** Python
- **Frameworks:** Flask, OPC UA Client
- **Datenbanken:** SQLite oder PostgreSQL
- **Bibliotheken:** opcua, requests, flask_sqlalchemy

## Architektur
Das System ist in mehrere Komponenten unterteilt:
1. **Web-API:** Eine RESTful API, die es Benutzern ermöglicht, mit dem System zu interagieren.
2. **Datenbank:** Speichert alle relevanten Daten und Zustände des Systems.
3. **OPC UA Client:** Kommuniziert mit den OPC UA-Variablen und empfängt Daten.

### Diagramm
![Systemarchitektur Diagramm](link-to-diagram)

## Setup-Anleitung
### Voraussetzungen
- Python 3.x
- pip (Python Paketmanager)
- PostgreSQL oder SQLite

### Installation
1. Klone das Repository:
   ```bash
   git clone https://github.com/mrgomulus/At.git
   cd At
   ```
2. Installiere die Abhängigkeiten:
   ```bash
   pip install -r requirements.txt
   ```
3. Setze die Datenbank auf:
   ```bash
   python setup_database.py
   ```
4. Starte den Server:
   ```bash
   python app.py
   ```

## Implementierungsrichtlinien
- **Code-Qualität:** Befolge die PEP 8 Richtlinien.
- **Dokumentation:** Jeder Codeabschnitt sollte ausreichend kommentiert sein.
- **Tests:** Schreibe Unit- und Integrationstests für kritische Funktionen.

## Fazit
Mit dieser Dokumentation und den bereitgestellten Informationen sollte jeder in der Lage sein, das Disruption Management System mit OPC UA Variablenintegration nachzubauen. Für weitere Fragen oder Feedback, wenden Sie sich bitte an den Projektleiter.