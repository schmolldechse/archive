# Production Compose

The four Compose files follow the separate database, API, and frontend layout
used by Phoc No. MinIO and its `mc` client are built from upstream source in
`minio/Dockerfile`; no prebuilt MinIO container image is pulled.

The stack contains PostgreSQL, MinIO, a one-shot bucket initializer, the API,
the capture worker, and the frontend. Only the API and frontend need reverse
proxy routes. PostgreSQL and MinIO have no published host ports. MinIO's console
is also internal; expose it only through a protected administrative route if
needed.

## Deployment

The Compose files join the external Docker network `archive`. Create it once on
the deployment server before starting any of the services:

```sh
docker network create archive
```

From the repository root, copy `deploy/production/.env.example` to
`deploy/production/.env` and set unique secrets and real HTTPS origins.
Keep `.env` out of Git. The production Compose files
do not read the root local-development `.env`.

```sh
docker compose --env-file deploy/production/.env -p archive-production \
  -f deploy/production/database.compose.yml \
  -f deploy/production/minio.compose.yml \
  -f deploy/production/api.compose.yml \
  -f deploy/production/frontend.compose.yml config --quiet

docker compose --env-file deploy/production/.env -p archive-production \
  -f deploy/production/database.compose.yml \
  -f deploy/production/minio.compose.yml \
  -f deploy/production/api.compose.yml \
  -f deploy/production/frontend.compose.yml up -d --build
```

Route the public frontend origin to `frontend:3000` and the API origin to
`api:8080` through a reverse proxy attached to the same Docker network.
`PUBLIC_API_BASE_URL` is embedded during the frontend build, so rebuild the
frontend when that URL changes. The API applies database migrations at startup;
the worker waits for a healthy API and does not run migrations itself.

The `minio-init` service creates the snapshot bucket once MinIO is healthy.
The `minio-data` and `database-data` volumes require coordinated backups.
See the main [production checklist](../../docs/configuration.md#production-checklist)
for application exposure, worker egress, and archived-content controls.
