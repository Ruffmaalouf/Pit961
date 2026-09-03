#!/usr/bin/env bash
set -euo pipefail

# Milestone 1 (P2-WP2/P2-WP3 frontend) local dev stack bootstrap.
#
# Brings up a REAL PostgreSQL instance, applies EF Core migrations for both
# DbContexts, and prepares everything the backend needs to seed its
# development account on first boot. This script deliberately does NOT start
# the backend or frontend itself — see the printed instructions at the end —
# because they are meant to run in their own terminal windows that you keep
# open, the same way any local dev server normally works.
#
# Safe to re-run: Postgres init/start and `dotnet ef database update` are
# both idempotent.

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BACKEND_DIR="$REPO_ROOT/backend"
DB_NAME="pit961_dev"
PG_PORT="5432"
PG_DATA_DIR="${PIT961_PG_DATA_DIR:-$HOME/pit961-pgdata}"

echo "== PIT961 Milestone 1 local dev stack =="
echo "Repo root:   $REPO_ROOT"
echo "Postgres data dir: $PG_DATA_DIR"
echo

# --- 1. Resolve a local, root-free PostgreSQL 16 (via the pgserver PyPI
#        package) unless the caller already has one on PATH. -----------------
if command -v pg_ctl >/dev/null 2>&1 && command -v initdb >/dev/null 2>&1; then
  PG_BIN_DIR="$(dirname "$(command -v pg_ctl)")"
  echo "Using system PostgreSQL binaries at $PG_BIN_DIR"
else
  echo "No system PostgreSQL found on PATH — using the portable pgserver package."
  if ! python3 -c "import pgserver" >/dev/null 2>&1; then
    echo "Installing pgserver (root-free PostgreSQL 16 binaries)..."
    pip install --user --break-system-packages pgserver >/dev/null
  fi
  PG_BIN_DIR="$(python3 -c "import pgserver, os; print(os.path.join(os.path.dirname(pgserver.__file__), 'pginstall', 'bin'))")"
  PG_LIB_DIR="$(python3 -c "import pgserver, os; print(os.path.join(os.path.dirname(pgserver.__file__), 'pginstall', 'lib'))")"
  export LD_LIBRARY_PATH="${PG_LIB_DIR}${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
  echo "Using pgserver PostgreSQL binaries at $PG_BIN_DIR"
fi

# --- 2. Init + start Postgres, idempotently. --------------------------------
if [ ! -s "$PG_DATA_DIR/PG_VERSION" ]; then
  echo "Initializing a fresh PostgreSQL data directory..."
  "$PG_BIN_DIR/initdb" -D "$PG_DATA_DIR" -U postgres --auth=trust >/dev/null
  {
    echo "listen_addresses = '127.0.0.1'"
  } >> "$PG_DATA_DIR/postgresql.conf"
  echo "host all all 127.0.0.1/32 trust" >> "$PG_DATA_DIR/pg_hba.conf"
fi

if ! "$PG_BIN_DIR/pg_ctl" -D "$PG_DATA_DIR" status >/dev/null 2>&1; then
  echo "Starting PostgreSQL on port $PG_PORT..."
  "$PG_BIN_DIR/pg_ctl" -D "$PG_DATA_DIR" -l "$PG_DATA_DIR/logfile" \
    -o "-p $PG_PORT -k $PG_DATA_DIR" start
else
  echo "PostgreSQL is already running."
fi

for _ in $(seq 1 20); do
  if "$PG_BIN_DIR/psql" -h 127.0.0.1 -p "$PG_PORT" -U postgres -c "SELECT 1;" >/dev/null 2>&1; then
    break
  fi
  sleep 0.5
done
"$PG_BIN_DIR/psql" -h 127.0.0.1 -p "$PG_PORT" -U postgres -c "SELECT 1;" >/dev/null
echo "PostgreSQL is reachable at 127.0.0.1:$PG_PORT."

# --- 3. Local-only startup config the app validates on boot (ValidateOnStart)
#        and appsettings.Development.json intentionally ships empty:
#          - Jwt:SigningKey (>= 32 bytes, a real CSPRNG value)
#          - Resend:ApiKey (Program.cs requires it non-blank; a placeholder is
#            fine for local dev — nothing here actually sends an email unless
#            a real flow calls Resend, and forgot-password is a P2-WP4+ concern)
#        The `dotnet user-secrets` path the backend README also documents
#        needs a <UserSecretsId> declared in GarageOS.Api.csproj, which does
#        not exist yet (adding one is a project-file change out of this
#        script's scope), so this script uses the standard ASP.NET Core
#        environment variable config source instead — always loaded, no code
#        change needed. Written to a gitignored local env file (never to any
#        tracked appsettings file) so it survives between runs instead of
#        rotating on every start.
cd "$BACKEND_DIR/GarageOS.Api"
LOCAL_ENV_FILE=".local.dev.env"
if [ ! -s "$LOCAL_ENV_FILE" ]; then
  echo "Generating local-only dev startup config (JWT signing key, placeholder Resend key)..."
  SIGNING_KEY="$(openssl rand -base64 48)"
  cat > "$LOCAL_ENV_FILE" <<ENVEOF
Jwt__SigningKey=$SIGNING_KEY
Jwt__Issuer=pit961-local-dev
Jwt__Audience=pit961-local-dev
Resend__ApiKey=local-dev-placeholder-not-a-real-key
Resend__FromAddress=dev@pit961.local
ENVEOF
  if ! grep -qxF "$LOCAL_ENV_FILE" .gitignore 2>/dev/null; then
    echo "$LOCAL_ENV_FILE" >> .gitignore
  fi
fi
set -a
# shellcheck disable=SC1090
. "./$LOCAL_ENV_FILE"
set +a

# --- 4. EF Core migrations for both DbContexts. -----------------------------
echo "Restoring local dotnet tools (dotnet-ef)..."
dotnet tool restore >/dev/null

echo "Applying AppDbContext migrations..."
dotnet ef database update --context AppDbContext \
  --project "../GarageOS.Infrastructure/GarageOS.Infrastructure.csproj"

echo "Applying PlatformDbContext migrations..."
dotnet ef database update --context PlatformDbContext \
  --project "../GarageOS.Infrastructure/GarageOS.Infrastructure.csproj"

# --- 5. Migrations for the integration-test database too, so `dotnet test`
#        (GarageOS.Tests.Integration) works out of the box against the
#        README-documented default connection string. IntegrationTestFixture
#        resets rows between runs (Respawn) but never applies schema itself.
INTEGRATION_CONN="Host=127.0.0.1;Port=$PG_PORT;Database=pit961_integration_test;Username=postgres"
echo "Applying migrations to the integration-test database..."
dotnet ef database update --context AppDbContext --connection "$INTEGRATION_CONN" \
  --project "../GarageOS.Infrastructure/GarageOS.Infrastructure.csproj" >/dev/null
dotnet ef database update --context PlatformDbContext --connection "$INTEGRATION_CONN" \
  --project "../GarageOS.Infrastructure/GarageOS.Infrastructure.csproj" >/dev/null

cd "$REPO_ROOT"

cat <<'EOF'

== Database ready. Now start the backend and frontend, each in its own terminal window that you keep open: ==

Terminal 1 (backend, http://localhost:5289, Swagger at /swagger):
  cd backend/GarageOS.Api
  set -a && . ./.local.dev.env && set +a
  dotnet run

Terminal 2 (frontend, http://localhost:5173):
  cd frontend
  npm install   # first time only
  npm run dev

The backend seeds a real development login on first boot (Development
environment only) — see GarageOS.Infrastructure/Data/Seed/DevelopmentSeeder.cs
for the exact seeded email/password. Once both are running, open
http://localhost:5173 in your browser.

The integration-test database (pit961_integration_test) was also migrated, so
`dotnet test` from backend/ works immediately too.

EOF
