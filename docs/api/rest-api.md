# REST-API und öffentliche Verträge

Diese Dokumentation beschreibt den aktuellen Stand der HTTP-Schnittstellen in `Archive.Api`. Maßgeblich sind die Controller, die Minimal-API in `Program.cs`, die Request- und Response-Contracts sowie die tatsächlich konfigurierte Authentifizierung, Autorisierung und JSON-Serialisierung.

## Allgemeine Konventionen

- API-Routen beginnen mit `/api`; der Healthcheck liegt separat unter `/healthz`.
- JSON-Eigenschaften verwenden `camelCase`.
- Aufzählungen werden als Strings übertragen. Ganzzahlige Enum-Werte sind nicht erlaubt. Die meisten Werte sind großgeschrieben, zum Beispiel `APPROVED`; allein `SnapshotSortOrder` verwendet `asc` und `desc`.
- UUIDs werden als kanonische Strings und `DateTimeOffset`-Werte als ISO-8601-Zeitstempel mit Offset übertragen.
- Optionale Werte werden als `null` übertragen, sofern der jeweilige Contract sie enthält.
- Öffentliche Endpunkte verwenden die CORS-Policy `public` und erlauben beliebige Origins, Header und Methoden.
- Session-, Benutzer- und Admin-Endpunkte erlauben ausschließlich den konfigurierten Frontend-Origin, Credentials und exponieren `ETag`.
- Antworten unter `/api/admin` und `/api/users/snapshots` erhalten `Cache-Control: private, no-store`.
- Schreibende Admin- und Submission-Aktionen prüfen den exakten `Origin` des konfigurierten Frontends. Zusätzlich verlangen sie genau einen starken, nicht als `*` formulierten `If-Match`-ETag.
- Ein `401 Unauthorized` bedeutet, dass keine gültige lokale Sitzung vorliegt. Ein `403 Forbidden` bedeutet, dass die Sitzung gültig ist, die benötigte Berechtigung oder Eigentümerschaft aber fehlt.

### Fehlerformat

Strukturierte Fehler folgen `application/problem+json`. Je nach Fehler können einzelne optionale RFC-7807-Felder fehlen.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Action unavailable",
  "status": 409,
  "detail": "This snapshot is no longer eligible for this action.",
  "instance": "/api/admin/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/review",
  "code": "snapshot_not_reviewable"
}
```

Validierungsfehler enthalten ein Feld-zu-Meldungen-Dictionary:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "title": [
      "A title is required."
    ]
  },
  "traceId": "00-…"
}
```

Einige einfache `NotFound()`, `Unauthorized()` und `Forbid()`-Ergebnisse besitzen keinen JSON-Body. Die bei einem Endpoint aufgeführten Statuscodes sind daher verbindlicher als die Annahme, dass jeder Fehler einen `ProblemDetails`-Body liefert.

## Übersicht

| Controller | Methode | Route | Zugriff |
| --- | --- | --- | --- |
| Minimal API | `GET` | `/healthz` | öffentlich |
| `AuthenticationController` | `GET` | `/api/auth/providers` | öffentlich |
| `AuthenticationController` | `GET` | `/api/auth/session` | öffentlich; wertet Session aus |
| `AuthenticationController` | `POST` | `/api/auth/login/{provider}` | öffentlich |
| `AuthenticationController` | `POST` | `/api/auth/logout` | öffentlich; entfernt vorhandene Session |
| `UserController` | `GET` | `/api/users/profile` | angemeldet |
| `UserController` | `PUT` | `/api/users/profile` | angemeldet |
| `ProfileImageController` | `GET` | `/api/users/profile-image` | angemeldet |
| `ProfileImageController` | `GET` | `/api/users/{handle}/profile-image` | öffentlich |
| `SnapshotsController` | `GET` | `/api/snapshots` | öffentlich |
| `SnapshotsController` | `GET` | `/api/snapshots/{snapshotId}` | öffentlich |
| `SnapshotsController` | `GET` | `/api/snapshots/{snapshotId}/content/{**path}` | öffentlich |
| `SnapshotsController` | `GET` | `/api/snapshots/{snapshotId}/screenshot` | öffentlich |
| `SnapshotsController` | `POST` | `/api/snapshots` | öffentlich; optionale Session |
| `SnapshotsController` | `GET` | `/api/snapshots/{snapshotId}/status` | öffentlich |
| `SnapshotsController` | `GET` | `/api/snapshots/{snapshotId}/events` | öffentlich |
| `SnapshotsController` | `POST` | `/api/snapshots/{snapshotId}/cancel` | öffentlich |
| `User.SnapshotsController` | `GET` | `/api/users/snapshots` | registrierter Benutzer |
| `User.SnapshotsController` | `GET` | `/api/users/snapshots/{id}` | Eigentümer |
| `User.SnapshotsController` | `GET` | `/api/users/snapshots/{id}/history` | Eigentümer |
| `User.SnapshotsController` | `GET` | `/api/users/snapshots/{id}/screenshot` | Eigentümer |
| `User.SnapshotsController` | `GET` | `/api/users/snapshots/{id}/content/{**path}` | Eigentümer |
| `User.SnapshotsController` | `POST` | `/api/users/snapshots/{id}/resubmit` | Eigentümer |
| `Admin.PermissionsController` | `GET` | `/api/admin/permissions` | `admin.access` und `admin.users.access` |
| `Admin.UsersController` | `GET` | `/api/admin/users` | `admin.access` und `admin.users.access` |
| `Admin.UsersController` | `GET` | `/api/admin/users/{handle}/permissions` | `admin.access` und `admin.users.access` |
| `Admin.UsersController` | `PATCH` | `/api/admin/users/{handle}/permissions` | Basisrechte plus aktionsabhängige Rechte |
| `Admin.SnapshotsController` | `GET` | `/api/admin/snapshots` | `admin.access` und `admin.snapshots.access` |
| `Admin.SnapshotsController` | `GET` | `/api/admin/snapshots/{id}` | dieselben Rechte |
| `Admin.SnapshotsController` | `GET` | `/api/admin/snapshots/{id}/history` | dieselben Rechte |
| `Admin.SnapshotsController` | `GET` | `/api/admin/snapshots/{id}/screenshot` | dieselben Rechte |
| `Admin.SnapshotsController` | `GET` | `/api/admin/snapshots/{id}/content/{**path}` | dieselben Rechte |
| `Admin.SnapshotsController` | `POST` | `/api/admin/snapshots/{id}/review` | dieselben Rechte |

`SnapshotSubmissionControllerBase` ist ein abstrakter Controller ohne eigene Route. Er stellt die gemeinsame Detail-, Screenshot-, ETag-, Origin- und Fehlerbehandlung für die Benutzer- und Admin-Snapshot-Controller bereit.

## Healthcheck

### `GET /healthz`

Der Endpoint prüft, ob der API-Prozess HTTP-Anfragen beantworten kann.

**Erfolg: `200 OK`**

```json
{
  "status": "ok"
}
```

## AuthenticationController

Basisroute: `/api/auth`. Alle Endpoints verwenden die credential-fähige Frontend-CORS-Policy.

### `GET /api/auth/providers`

Liefert die registrierten externen Anmeldeanbieter.

**Erfolg: `200 OK`**

```json
[
  {
    "key": "google",
    "displayName": "Google"
  }
]
```

`key` und `displayName` sind jeweils höchstens 64 Zeichen lang. Der Schlüssel wird bei der Login-Route unverändert verwendet.

### `GET /api/auth/session`

Liefert den Zustand der aktuellen Cookie-Session. Die Antwort wird nicht gecacht. Bei einer authentifizierten Session mit ungültiger oder gelöschter Benutzer-ID antwortet die API mit `401`.

**Anonyme Antwort: `200 OK`**

```json
{
  "permissions": [],
  "isAuthenticated": false,
  "user": null
}
```

**Authentifizierte Antwort: `200 OK`**

```json
{
  "permissions": [
    "admin.access",
    "admin.snapshots.access",
    "snapshots.publish_without_approval"
  ],
  "isAuthenticated": true,
  "user": {
    "role": "ADMIN",
    "handle": "archive-admin",
    "displayName": "Archive Admin",
    "profileImageUrl": "https://api.example.test/api/users/archive-admin/profile-image",
    "isRegistrationComplete": true
  }
}
```

`permissions` enthält die effektiv erlaubten Schlüssel in Katalogreihenfolge. Bei anonymen oder noch nicht vollständig registrierten Konten ist die Liste leer. Mehrere für eine Aktion erforderliche Schlüssel müssen gemeinsam geprüft werden.

### `POST /api/auth/login/{provider}`

Startet den externen OAuth/OIDC-Login. Der Request wird als `application/x-www-form-urlencoded` aus dem Browser gesendet. `provider` muss einem Schlüssel aus `/api/auth/providers` entsprechen. Der optionale Query-Parameter `returnUrl` ist ein lokaler Frontend-Pfad mit maximal 2.048 Zeichen; ungültige oder rekursive Auth-Pfade fallen auf `/` zurück.

Beispiel:

```http
POST /api/auth/login/google?returnUrl=%2Faccount HTTP/1.1
Content-Type: application/x-www-form-urlencoded
Origin: https://archive.example.test
```

Der Erfolgsfall ist `302 Found` zum externen Provider. Unbekannte Provider liefern `404`, unzulässige Browser-Ursprünge `403` und ungültige Eingaben `400`.

### `POST /api/auth/logout`

Entfernt das lokale Session-Cookie. Eine externe Provider-Session bleibt bestehen. Der Endpoint liefert `303 See Other` mit `Location` auf die Frontend-Startseite und keinen JSON-Body.

## UserController

Basisroute: `/api/users`. Beide Endpoints erfordern eine gültige Session.

### `GET /api/users/profile`

Liefert das aktuelle Benutzerprofil.

**Erfolg: `200 OK`**

```json
{
  "role": "USER",
  "handle": "jane-doe",
  "displayName": "Jane Doe",
  "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
  "isRegistrationComplete": true
}
```

Vor Abschluss der Registrierung können `handle`, `displayName` und `profileImageUrl` `null` sein. `role` ist `USER` oder `ADMIN`.

### `PUT /api/users/profile`

Speichert das normalisierte Profil und schließt die Registrierung ab.

**Request: `application/json`**

```json
{
  "handle": "jane-doe",
  "displayName": "Jane Doe"
}
```

- `handle`: 4 bis 32 Zeichen, Kleinschreibung nach Normalisierung; Buchstaben oder Ziffern außen, Bindestriche und Unterstriche nur innerhalb.
- `displayName`: 6 bis 64 Zeichen, getrimmt und Unicode-NFC-normalisiert; keine Steuerzeichen.

**Erfolg: `200 OK`** verwendet den `UserProfileResponse` aus dem vorherigen Abschnitt. Ein bereits verwendeter Handle liefert `409` als Validierungsproblem mit dem Feld `handle`. Weitere mögliche Antworten sind `400`, `401` und `415`.

## ProfileImageController

Diese Endpoints liefern Binärdaten und daher keinen JSON-Erfolgsvertrag.

### `GET /api/users/profile-image`

Liefert das gespeicherte WebP-Bild des angemeldeten Benutzers mit `Content-Type: image/webp`, `X-Content-Type-Options: nosniff` und einem starken, aus dem Inhalt gebildeten `ETag`. `If-None-Match` kann `304 Not Modified` erzeugen. Die Antwort ist privat und wird nicht gespeichert. Ohne Bild folgt `404`, bei einer ungültigen Session `401`.

### `GET /api/users/{handle}/profile-image`

Liefert das öffentliche WebP-Bild eines vollständig registrierten Benutzers. Der Handle ist der aktuelle eindeutige, kleingeschriebene Handle. Unbekannte oder unvollständige Konten und Konten ohne Bild liefern `404`. Auch hier werden `ETag`, `If-None-Match`, `304` und `X-Content-Type-Options: nosniff` unterstützt.

## Öffentlicher SnapshotsController

Basisroute: `/api/snapshots`. Metadaten, Inhalte und Screenshots werden nur für gespeicherte und genehmigte Snapshots veröffentlicht. Andere Moderations- oder Verarbeitungszustände erscheinen gegenüber der öffentlichen API als `404`.

### `GET /api/snapshots`

Durchsucht das öffentliche Archiv.

**Query-Parameter**

| Parameter | Typ | Bedeutung |
| --- | --- | --- |
| `text` | string, max. 500 | Leerzeichengetrennte Begriffe; alle müssen in Titel, Beschreibung, URL oder Tags vorkommen; max. 20 Begriffe. |
| `sourceUrl` | URL, max. 4.096 | Exakte normalisierte Quell-URL. |
| `title` | string, max. 512 | Groß-/kleinschreibungsunabhängiger Teilstring. |
| `description` | string, max. 2.048 | Groß-/kleinschreibungsunabhängiger Teilstring. |
| `tags` | wiederholter string | Bis zu 30 verschiedene Tags mit je max. 128 Zeichen; alle müssen passen. |
| `sourceType` | enum | `URL`, `HTML`, `MHTML` oder `WEBARCHIVE`. |
| `quality` | enum | `COMPLETE` oder `INCOMPLETE`. |
| `capturedFrom` | DateTimeOffset | Inklusive Untergrenze. |
| `capturedUntil` | DateTimeOffset | Exklusive Obergrenze. |
| `order` | enum | `desc` als Standard oder `asc`; ID als stabiler Tie-Breaker in derselben Richtung. |
| `page` | integer | 1 bis 1.000.000; Standard 1. |
| `pageSize` | integer | 1 bis 100; Standard 24. |

**Erfolg: `200 OK`**

```json
{
  "items": [
    {
      "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
      "sourceUrl": "https://example.com/article",
      "title": "Example article",
      "description": "Archived for long-term reference.",
      "sourceType": "URL",
      "quality": "COMPLETE",
      "createdAt": "2026-10-18T10:15:30+00:00",
      "durationMilliseconds": 1842,
      "storageBytes": 482901,
      "resourceCount": 37,
      "tags": ["example", "history"],
      "screenshotUrl": "https://api.example.test/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/screenshot",
      "contentUrl": "https://api.example.test/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/content/",
      "createdBy": {
        "handle": "jane-doe",
        "displayName": "Jane Doe",
        "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
        "role": "USER"
      },
      "captureStartedAt": "2026-10-18T10:15:31+00:00",
      "captureCompletedAt": "2026-10-18T10:15:33+00:00"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 24,
  "indexTotal": 842,
  "recentCaptureTimes": [
    "2026-10-18T10:15:30+00:00"
  ],
  "captureDistribution": {
    "total": 42,
    "firstYear": 2024,
    "lastYear": 2026,
    "years": [
      { "year": 2024, "count": 9 },
      { "year": 2025, "count": 14 },
      { "year": 2026, "count": 19 }
    ]
  }
}
```

`total` berücksichtigt alle Filter. `indexTotal` ist die Gesamtzahl aller veröffentlichten Snapshots unabhängig von Filtern. `recentCaptureTimes` enthält bis zu vier neueste öffentliche Erstellungszeiten. `captureDistribution` berücksichtigt Text- und Metadatenfilter, aber keine Capture-Zeitfilter und keine Pagination.

### `GET /api/snapshots/{snapshotId}`

Liefert einen einzelnen öffentlichen `SnapshotResponse`, wie er im `items`-Array oben dargestellt ist. `sourceUrl`, `description`, `createdBy` und dessen `profileImageUrl` dürfen `null` sein. Für einen veröffentlichten Snapshot sind die Capture-Metriken und öffentlichen Ressourcen normalerweise gesetzt. Nicht veröffentlichte oder unbekannte IDs liefern `404`.

### `GET /api/snapshots/{snapshotId}/content/{**path}`

Liefert die archivierte Startseite bei leerem `path` oder eine gespeicherte Ressource. Der konkrete `Content-Type` richtet sich nach der Ressource. Original hochgeladene Quelldateien werden als `application/octet-stream` und Attachment ausgeliefert. Byte-Range-Requests können `206 Partial Content` oder `416 Range Not Satisfiable` erzeugen. Ungültige beziehungsweise traversierende Pfade liefern `400`, fehlende oder nicht öffentliche Inhalte `404`.

Die Antwort setzt eine restriktive Content-Security-Policy, verbietet Skripte, Netzwerkverbindungen, Objekte und Formulare und setzt `Referrer-Policy: no-referrer` sowie `X-Content-Type-Options: nosniff`.

### `GET /api/snapshots/{snapshotId}/screenshot`

Liefert den öffentlichen Screenshot als `image/png`. Unbekannte, nicht gespeicherte oder nicht genehmigte Snapshots liefern `404`.

### `POST /api/snapshots` mit JSON

Reicht eine öffentliche HTTP(S)-URL zur Verarbeitung ein. Eine Session ist optional. Registrierte Benutzer mit `snapshots.publish_without_approval` starten mit `APPROVED`; andere angemeldete und anonyme Einreichungen benötigen eine Moderation.

**Request: `application/json`**

```json
{
  "sourceUrl": "https://example.com/article",
  "title": "Example article",
  "description": "Optional context for the archive.",
  "tags": ["Example", "#History"]
}
```

`sourceUrl` ist eine öffentliche HTTP(S)-URL mit maximal 4.096 Zeichen. `title` ist erforderlich und maximal 512 Zeichen lang. `description` ist optional und maximal 2.048 Zeichen lang. Es sind höchstens 30 Tags zu je 128 Zeichen erlaubt. Tags werden getrimmt, von führenden `#` befreit, kleingeschrieben und dedupliziert.

### `POST /api/snapshots` mit Multipart

Die zweite Repräsentation desselben Endpoints akzeptiert `multipart/form-data` für `.html`, `.mhtml` oder `.webarchive`.

| Formularfeld | Erforderlich | Inhalt |
| --- | --- | --- |
| `file` | ja | Nicht leere unterstützte Datei. |
| `sourceUrl` | ja | Ursprüngliche öffentliche HTTP(S)-URL. |
| `title` | ja | Titel nach denselben Regeln wie beim JSON-Request. |
| `description` | nein | Optionale Beschreibung. |
| `tags` | nein | Für mehrere Tags wiederholt senden. |

Eine rein konzeptionelle JSON-Darstellung des öffentlichen Vertrags lautet:

```json
{
  "file": "<binary .html, .mhtml or .webarchive>",
  "sourceUrl": "https://example.com/article",
  "title": "Uploaded article",
  "description": null,
  "tags": ["upload", "history"]
}
```

Diese Darstellung darf nicht als JSON gesendet werden; die Datei erfordert Multipart.

**Gemeinsame Erfolgsantwort: `202 Accepted`**

```json
{
  "moderationStatus": "PENDING",
  "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
  "statusUrl": "/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/status",
  "snapshotUrl": "/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531"
}
```

Die `Location`-Antwort verweist auf `statusUrl`. `snapshotUrl` bleibt bis zur erfolgreichen Speicherung und Genehmigung `404`. Mögliche Fehler sind `400`, `401` bei einem nicht verfügbaren angemeldeten Konto, `413` bei zu großen Uploads und `415` bei nicht unterstützten Medientypen.

### `GET /api/snapshots/{snapshotId}/status`

Liefert den aktuellen Verarbeitungs- und Moderationsstatus. Dieser Fortschrittsendpoint bleibt auch für anonyme Einreichungen öffentlich erreichbar.

```json
{
  "moderationStatus": "PENDING",
  "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
  "status": "RUNNING",
  "progressStep": "LOADING_PAGE",
  "queuePosition": null,
  "canCancel": true,
  "cancelRequested": false,
  "startedAt": "2026-10-18T10:15:31+00:00"
}
```

`queuePosition` ist nur für wartende Einträge gesetzt. `startedAt` bleibt vor dem Verarbeitungsbeginn `null`. Eine unbekannte ID liefert `404`.

### `GET /api/snapshots/{snapshotId}/events`

Öffnet einen `text/event-stream`. Normale Statusaktualisierungen werden als unbenannte SSE-Nachrichten gesendet, deren `data` exakt dem `SnapshotProcessingResponse` des Statusendpoints entspricht:

```text
data: {"moderationStatus":"PENDING","id":"018f4d8e-62c1-7e23-a663-9a3fc0df7531","status":"RUNNING","progressStep":"LOADING_PAGE","queuePosition":null,"canCancel":true,"cancelRequested":false,"startedAt":"2026-10-18T10:15:31+00:00"}

```

Keep-alives verwenden `: heartbeat`. Streamfehler werden als benanntes Ereignis übertragen:

```text
event: error
data: {"status":404,"title":"Snapshot processing state unavailable"}

```

### `POST /api/snapshots/{snapshotId}/cancel`

Fordert den Abbruch einer noch abbrechbaren Verarbeitung an. Der Endpoint ist öffentlich und benötigt keinen JSON-Body. Die Erfolgsantwort verwendet den aktuellen `SnapshotProcessingResponse`. Eine unbekannte ID liefert `404`; ein nicht mehr abbrechbarer Zustand `409`.

## User.SnapshotsController

Basisroute: `/api/users/snapshots`. Alle Endpoints verlangen eine gültige Session und ein bereits vollständig registriertes Archivkonto. Alle Ressourcen sind zusätzlich auf den Eigentümer der Einreichung beschränkt.

### `GET /api/users/snapshots`

Listet eigene Einreichungen. Unterstützt `q` mit maximal 500 Zeichen für Titel oder Quell-URL, den optionalen exakten `moderationStatus`, `page` von 1 bis 1.000.000 und `pageSize` von 1 bis 100. Standardwerte sind 1 und 25. Ohne Statusfilter werden alle eigenen Zustände angezeigt.

```json
{
  "items": [
    {
      "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
      "title": "Example article",
      "sourceUrl": "https://example.com/article",
      "sourceType": "URL",
      "createdAt": "2026-10-18T10:15:30+00:00",
      "moderationUpdatedAt": "2026-10-18T10:20:00+00:00",
      "moderationStatus": "REJECTED",
      "processingStatus": "COMPLETED",
      "quality": "COMPLETE",
      "createdBy": {
        "handle": "jane-doe",
        "displayName": "Jane Doe",
        "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
        "role": "USER"
      }
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 25
}
```

### `GET /api/users/snapshots/{id}`

Liefert private Details der eigenen Einreichung und einen starken `ETag`-Header.

```json
{
  "snapshot": {
    "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
    "sourceUrl": "https://example.com/article",
    "title": "Example article",
    "description": null,
    "sourceType": "URL",
    "quality": "COMPLETE",
    "createdAt": "2026-10-18T10:15:30+00:00",
    "durationMilliseconds": 1842,
    "storageBytes": 482901,
    "resourceCount": 37,
    "tags": ["example"],
    "screenshotUrl": "https://api.example.test/api/users/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/screenshot",
    "contentUrl": "https://api.example.test/api/users/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/content/index.html",
    "createdBy": {
      "handle": "jane-doe",
      "displayName": "Jane Doe",
      "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
      "role": "USER"
    },
    "captureStartedAt": "2026-10-18T10:15:31+00:00",
    "captureCompletedAt": "2026-10-18T10:15:33+00:00"
  },
  "moderation": {
    "status": "REJECTED",
    "updatedAt": "2026-10-18T10:20:00+00:00",
    "lastDecisionReason": "The submitted source does not match the captured page."
  },
  "processingStatus": "COMPLETED"
}
```

Sobald die Capture-Daten gespeichert sind, verweisen `screenshotUrl` und `contentUrl` auf die geschützten Ressourcen unter `/api/users/snapshots/{id}`. Dadurch kann der Eigentümer die Einreichung vor einer Genehmigung prüfen, während die öffentlichen `/api/snapshots/{id}`-Ressourcen weiterhin `404` liefern. Fremde oder unbekannte Einreichungen liefern ebenfalls `404`.

### `GET /api/users/snapshots/{id}/history`

Liefert die allgemeine, sequenzielle Snapshot-Historie. `page` und `pageSize` entsprechen den Listengrenzen. Eigentümer sehen keine Admin-Identität; `actor` ist hier daher `null`.

```json
{
  "items": [
    {
      "sequence": 1,
      "createdAt": "2026-10-18T10:15:30+00:00",
      "actor": null,
      "content": {
        "type": "snapshot.submitted",
        "version": 1,
        "data": {}
      }
    },
    {
      "sequence": 2,
      "createdAt": "2026-10-18T10:20:00+00:00",
      "actor": null,
      "content": {
        "type": "snapshot.moderated",
        "version": 1,
        "data": {
          "previousStatus": "PENDING",
          "status": "REJECTED",
          "reason": "The submitted source does not match the captured page."
        }
      }
    }
  ],
  "total": 2,
  "page": 1,
  "pageSize": 25
}
```

`content.type` ist absichtlich ein offener String. Clients müssen unbekannte Typen und zusätzliche Felder in `data` tolerieren. `version` versioniert die jeweilige Payload-Struktur. Aktuell bekannte Version-1-Inhalte stehen im Abschnitt „Snapshot-Historie“.

### `GET /api/users/snapshots/{id}/screenshot`

Liefert die private Vorschau der eigenen gespeicherten Einreichung als `image/png`. Ohne gespeicherten Screenshot oder bei einer fremden ID folgt `404`.

### `GET /api/users/snapshots/{id}/content/{**path}`

Liefert gespeicherte Inhalte der eigenen Einreichung vor oder nach der Moderation. Eine gültige Session ist für die Startseite und jede eingebettete Ressource erforderlich; die Eigentümerschaft wird bei jedem Request erneut geprüft. Die Content-Security-Policy beschränkt Ressourcen und Unterframes auf den Content-Pfad desselben Snapshots. Dokumentnavigationen müssen aus dem konfigurierten Frontend oder aus demselben Snapshot stammen. Dafür verwendet die private Antwort `Referrer-Policy: same-origin`; Range-Unterstützung und Pfadvalidierung entsprechen dem öffentlichen Content-Endpoint.

### `POST /api/users/snapshots/{id}/resubmit`

Fordert für einen abgelehnten Snapshot eine erneute Prüfung ohne inhaltliche Änderungen an. Der Request hat keinen Body und verlangt den konfigurierten `Origin` sowie genau einen starken `If-Match` aus dem vorherigen Detail-GET.

```http
POST /api/users/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/resubmit HTTP/1.1
Origin: https://archive.example.test
If-Match: "018f4d8e62c17e23"
Content-Length: 0
```

`200 OK` liefert den aktualisierten `SnapshotSubmissionDetailResponse` mit `moderationStatus: "RESUBMITTED"`. Ein Body erzeugt `400`; weitere mögliche Antworten sind `403`, `404`, `409`, `412` und `428`.

## Admin.PermissionsController

Basisroute: `/api/admin/permissions`. Zugriff verlangt die Rolle `ADMIN` sowie gemeinsam `admin.access` und `admin.users.access`.

### `GET /api/admin/permissions`

Liefert den stabil sortierten Berechtigungskatalog ohne interne Rollenvorgaben.

```json
{
  "items": [
    {
      "key": "admin.access",
      "displayName": "Access admin portal",
      "description": "Open the admin portal. Access to individual sections and administrative actions requires their respective permissions."
    },
    {
      "key": "admin.users.access",
      "displayName": "Access user administration",
      "description": "Open the Users section and view user profiles, roles, and individual permission settings. Changing access requires the corresponding management permission."
    },
    {
      "key": "admin.snapshots.access",
      "displayName": "Access snapshot moderation",
      "description": "View submissions, previews, and snapshot history. Approve or reject snapshots with an optional reason. Requires access to the admin portal."
    },
    {
      "key": "snapshots.publish_without_approval",
      "displayName": "Publish without review",
      "description": "Publish your own snapshot submissions once capture has completed, without waiting for a reviewer to approve them."
    },
    {
      "key": "users.assign_roles",
      "displayName": "Manage user roles",
      "description": "Change another user's role between User and Admin. The role determines default permissions; existing personal overrides remain unchanged. Requires access to the admin portal and user administration."
    },
    {
      "key": "users.assign_permissions",
      "displayName": "Manage permission overrides",
      "description": "Set, replace, or remove another user's individual permission overrides. Allow grants a permission, Deny blocks it, and removing an override restores the role default. Requires access to the admin portal and user administration."
    }
  ]
}
```

## Admin.UsersController

Basisroute: `/api/admin/users`. Alle Endpoints verlangen `admin.access` und `admin.users.access`.

### `GET /api/admin/users`

Listet vollständig registrierte Benutzer nach Handle sortiert. `q` durchsucht Handle und Anzeigename als Teilstring und ist auf 128 Zeichen beschränkt. `role` ist optional `USER` oder `ADMIN`. `page` reicht von 1 bis 10.000, `pageSize` von 1 bis 100; Standards sind 1 und 25.

```json
{
  "items": [
    {
      "role": "USER",
      "handle": "jane-doe",
      "displayName": "Jane Doe",
      "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
      "isRegistrationComplete": true
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 25
}
```

### `GET /api/admin/users/{handle}/permissions`

Liefert Rolle, persönliche Overrides und effektive Werte für alle bekannten Berechtigungen. Die Antwort enthält einen starken `ETag`.

```json
{
  "handle": "jane-doe",
  "displayName": "Jane Doe",
  "role": "USER",
  "permissions": [
    {
      "key": "admin.access",
      "defaultAllowed": false,
      "override": null,
      "allowed": false
    },
    {
      "key": "snapshots.publish_without_approval",
      "defaultAllowed": false,
      "override": "ALLOW",
      "allowed": true
    }
  ]
}
```

Die echte Antwort enthält jeden Katalogeintrag in stabiler Reihenfolge. `override` ist `ALLOW`, `DENY` oder `null`; `allowed` ist das Ergebnis aus Rollenvorgabe und persönlichem Override. Unbekannte Handles liefern `404` mit `code: "user_not_found"`.

### `PATCH /api/admin/users/{handle}/permissions`

Ändert Rolle und/oder einzelne Overrides atomar. Nicht genannte Werte bleiben unverändert. Unbekannte JSON-Felder sind nicht erlaubt.

```json
{
  "role": "ADMIN",
  "permissionOverrides": [
    {
      "permission": "admin.snapshots.access",
      "effect": "ALLOW"
    },
    {
      "permission": "snapshots.publish_without_approval",
      "effect": null
    }
  ]
}
```

Mindestens `role` oder ein Override-Eintrag ist erforderlich. Jeder bekannte Schlüssel darf höchstens einmal vorkommen. `effect: null` entfernt den gespeicherten Override und stellt damit die Rollenvorgabe wieder her. Eine Rollenänderung verlangt zusätzlich `users.assign_roles`; Override-Änderungen verlangen `users.assign_permissions`.

Der Request benötigt den konfigurierten `Origin` und einen starken `If-Match` aus dem GET. `200 OK` liefert den aktualisierten `UserAccessResponse` und einen neuen `ETag`. Selbständerungen und das Entfernen des letzten vollständig berechtigten Admins liefern `409`. Veraltete ETags liefern `412`, fehlende `428`, syntaktisch ungültige `400`.

## Admin.SnapshotsController

Basisroute: `/api/admin/snapshots`. Alle Endpoints verlangen gemeinsam `admin.access` und `admin.snapshots.access`.

### `GET /api/admin/snapshots`

Listet gespeicherte Einreichungen zur Moderation. Die Query-Parameter entsprechen der eigenen Liste: `q`, `moderationStatus`, `page` und `pageSize`. Ohne Statusfilter werden `PENDING` und `RESUBMITTED` angezeigt. Der JSON-Vertrag ist `SnapshotSubmissionListResponse`, wie im Benutzerabschnitt gezeigt.

### `GET /api/admin/snapshots/{id}`

Liefert private Snapshot-Details und einen starken `ETag`. Der Vertrag entspricht `SnapshotSubmissionDetailResponse`; Moderationsdaten sind unter `moderation` gruppiert. Gespeicherte Ressourcen verwenden geschützte URLs unter der Admin-Route.

### `GET /api/admin/snapshots/{id}/history`

Liefert die allgemeine Snapshot-Historie mit `page` und `pageSize`. Anders als in der Eigentümeransicht kann `actor` den Handle des handelnden Administrators enthalten:

```json
{
  "items": [
    {
      "sequence": 2,
      "createdAt": "2026-10-18T10:20:00+00:00",
      "actor": {
        "handle": "archive-admin",
        "displayName": "Archive Admin",
        "profileImageUrl": "https://api.example.test/api/users/archive-admin/profile-image",
        "role": "ADMIN"
      },
      "content": {
        "type": "snapshot.moderated",
        "version": 1,
        "data": {
          "previousStatus": "PENDING",
          "status": "APPROVED",
          "reason": "Capture and metadata verified."
        }
      }
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 25
}
```

### `GET /api/admin/snapshots/{id}/screenshot`

Liefert die private gespeicherte Vorschau als `image/png`. Fehlt die Einreichung oder der Screenshot, folgt `404`.

### `GET /api/admin/snapshots/{id}/content/{**path}`

Liefert gespeicherte Inhalte für die Moderationsprüfung. Die Session und beide Admin-Berechtigungen werden für die Startseite und jede eingebettete Ressource geprüft. Ressourcen, Unterframes und spätere Dokumentnavigationen bleiben auf den Content-Pfad desselben Snapshots begrenzt. Die private Antwort verwendet dafür `Referrer-Policy: same-origin`; Range-Unterstützung und Pfadvalidierung entsprechen dem öffentlichen Content-Endpoint.

### `POST /api/admin/snapshots/{id}/review`

Genehmigt oder verwirft einen prüfbaren Snapshot. Der Grund ist bei beiden Entscheidungen optional.

```json
{
  "decision": "APPROVED",
  "reason": "Capture and metadata verified."
}
```

`decision` akzeptiert ausschließlich `APPROVED` oder `REJECTED`; `reason` darf `null` sein und höchstens 2.048 Zeichen enthalten. Der Request verlangt den konfigurierten `Origin` und genau einen starken `If-Match` aus dem Detail-GET.

`200 OK` liefert den aktualisierten `SnapshotSubmissionDetailResponse` samt neuem Status. Ein nicht mehr prüfbarer Zustand liefert `409` mit `code: "snapshot_not_reviewable"`, ein veralteter ETag `412` mit `code: "snapshot_changed"`, ein fehlender ETag `428` und ein ungültiger ETag `400`.

## Wiederverwendbare Snapshot-Verträge

### `SnapshotResponse`

```json
{
  "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
  "sourceUrl": "https://example.com/article",
  "title": "Example article",
  "description": "Archived for reference.",
  "sourceType": "URL",
  "quality": "COMPLETE",
  "createdAt": "2026-10-18T10:15:30+00:00",
  "durationMilliseconds": 1842,
  "storageBytes": 482901,
  "resourceCount": 37,
  "tags": ["example"],
  "screenshotUrl": "https://api.example.test/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/screenshot",
  "contentUrl": "https://api.example.test/api/snapshots/018f4d8e-62c1-7e23-a663-9a3fc0df7531/content/",
  "createdBy": {
    "handle": "jane-doe",
    "displayName": "Jane Doe",
    "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image"
  },
  "captureStartedAt": "2026-10-18T10:15:31+00:00",
  "captureCompletedAt": "2026-10-18T10:15:33+00:00"
}
```

Nullable Felder sind `sourceUrl`, `description`, `quality`, `durationMilliseconds`, `storageBytes`, `resourceCount`, `screenshotUrl`, `contentUrl`, `createdBy`, `createdBy.profileImageUrl`, `captureStartedAt` und `captureCompletedAt`. Welche Werte privat sichtbar sind, hängt vom Verarbeitungszustand ab; öffentliche Endpoints liefern nur genehmigte, gespeicherte Snapshots.

### `SnapshotSubmissionSummary`

```json
{
  "id": "018f4d8e-62c1-7e23-a663-9a3fc0df7531",
  "title": "Example article",
  "sourceUrl": "https://example.com/article",
  "sourceType": "URL",
  "createdAt": "2026-10-18T10:15:30+00:00",
  "moderationUpdatedAt": "2026-10-18T10:20:00+00:00",
  "moderationStatus": "PENDING",
  "processingStatus": "COMPLETED",
  "quality": "COMPLETE",
  "createdBy": {
    "handle": "jane-doe",
    "displayName": "Jane Doe",
    "profileImageUrl": "https://api.example.test/api/users/jane-doe/profile-image",
    "role": "USER"
  }
}
```

`sourceUrl`, `quality` und `createdBy` können `null` sein. `createdBy` verwendet denselben öffentlichen Benutzervertrag wie Snapshot-Zuordnungen und Historienakteure.

### Snapshot-Historie

Jeder Eintrag besitzt eine innerhalb seines Snapshots fortlaufende `sequence`. `content` bildet einen erweiterbaren Envelope aus offenem `type`, positiver `version` und abstraktem JSON-Objekt `data`.

Bekannte Version-1-Ereignisse:

```json
{
  "type": "snapshot.submitted",
  "version": 1,
  "data": {}
}
```

```json
{
  "type": "snapshot.moderated",
  "version": 1,
  "data": {
    "previousStatus": "PENDING",
    "status": "REJECTED",
    "reason": "Optional reason"
  }
}
```

```json
{
  "type": "snapshot.resubmitted",
  "version": 1,
  "data": {
    "previousStatus": "REJECTED",
    "status": "RESUBMITTED"
  }
}
```

```json
{
  "type": "snapshot.stored",
  "version": 1,
  "data": {
    "quality": "COMPLETE",
    "durationMilliseconds": 1842,
    "storageBytes": 482901,
    "resourceCount": 37
  }
}
```

```json
{
  "type": "snapshot.processing_finished",
  "version": 1,
  "data": {}
}
```

Die Metriken des `snapshot.stored`-Ereignisses dürfen `null` sein. Neue Typen, neue Versionen und zusätzliche Datenfelder sind vertragsgemäß möglich.

## Enum-Werte

| Vertrag | Zulässige JSON-Werte |
| --- | --- |
| `ArchiveRole` | `USER`, `ADMIN` |
| `ArchivePermissionEffect` | `ALLOW`, `DENY` |
| `SnapshotModerationStatus` | `PENDING`, `APPROVED`, `REJECTED`, `RESUBMITTED` |
| `SourceType` | `URL`, `HTML`, `MHTML`, `WEBARCHIVE` |
| `SnapshotQuality` | `COMPLETE`, `INCOMPLETE` |
| `ProcessingStatus` | `QUEUED`, `RUNNING`, `COMPLETED`, `FAILED`, `CANCELLED`, `DISCARDED`, `ABORTED` |
| `ProgressStep` | `SOURCE_CHECK`, `LOADING_PAGE`, `LOADING_DYNAMIC_CONTENT`, `SAVING_SNAPSHOT`, `FINISHED` |
| `SnapshotSortOrder` | `desc`, `asc` |

## Autorisierungsmodell

| Berechtigung | Wirkung |
| --- | --- |
| `admin.access` | Öffnet das Admin-Portal; einzelne Bereiche verlangen weitere Berechtigungen. |
| `admin.users.access` | Erlaubt Zugriff auf Benutzerverwaltung und Berechtigungskatalog, jeweils zusammen mit `admin.access`. |
| `admin.snapshots.access` | Erlaubt Lesen und Moderieren von Einreichungen, zusammen mit `admin.access`. |
| `snapshots.publish_without_approval` | Genehmigt eigene neue Einreichungen automatisch nach erfolgreicher Speicherung. |
| `users.assign_roles` | Erlaubt Rollenänderungen innerhalb der Benutzerverwaltung. |
| `users.assign_permissions` | Erlaubt das Setzen und Entfernen persönlicher Berechtigungs-Overrides. |

Mehrere Attribute werden mit logischem UND ausgewertet. Persönliche `ALLOW`- und `DENY`-Overrides verändern die Rollenvorgabe für genau einen Schlüssel. Die vollständige Registrierung wird für die eigenen Snapshot-Endpoints zusätzlich über einen Filter erzwungen.
