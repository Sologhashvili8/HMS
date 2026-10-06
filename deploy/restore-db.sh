#!/usr/bin/env bash
# ლოკალური .bak-ის აღდგენა სერვერზე არსებულ SQL კონტეინერში.
# გამოყენება (HMS საქაღალდიდან):  ./deploy/restore-db.sh /root/hms.bak
set -euo pipefail

BAK="${1:?გამოყენება: ./deploy/restore-db.sh /path/to/hms.bak}"
cd "$(dirname "$0")/.."

C="docker-compose.prod.yml"
DB="HotelsManagementSystemDB"
SA_PASS="$(grep -E '^MSSQL_SA_PASSWORD=' .env | cut -d= -f2-)"
[ -n "$SA_PASS" ] || { echo ".env-ში MSSQL_SA_PASSWORD ცარიელია"; exit 1; }

sql() {
  docker compose -f "$C" exec -T sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASS" -b "$@"
}

echo "==> SQL კონტეინერს ვაწევ"
docker compose -f "$C" up -d sql
until sql -Q "SELECT 1" >/dev/null 2>&1; do sleep 3; done

echo "==> .bak-ს ვაკოპირებ კონტეინერში"
docker compose -f "$C" cp "$BAK" sql:/var/opt/mssql/data/restore.bak
docker compose -f "$C" exec -T -u root sql chmod 644 /var/opt/mssql/data/restore.bak

echo "==> ვკითხულობ ფაილების ლოგიკურ სახელებს"
FILES="$(sql -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/data/restore.bak'")"
DATA="$(echo "$FILES" | awk -F'|' '$3=="D"{print $1; exit}')"
LOG="$(echo "$FILES" | awk -F'|' '$3=="L"{print $1; exit}')"
[ -n "$DATA" ] && [ -n "$LOG" ] || { echo "ლოგიკური სახელები ვერ ვიპოვე:"; echo "$FILES"; exit 1; }

echo "==> ვაღდგენ $DB (data=$DATA, log=$LOG)"
sql -Q "RESTORE DATABASE [$DB] FROM DISK='/var/opt/mssql/data/restore.bak' WITH MOVE '$DATA' TO '/var/opt/mssql/data/$DB.mdf', MOVE '$LOG' TO '/var/opt/mssql/data/${DB}_log.ldf', REPLACE, STATS=20"

echo "==> სურათების ლოკალურ მისამართებს ვასუფთავებ (თუ იყო)"
sql -d "$DB" -Q "UPDATE Hotels SET ImageUrl = REPLACE(ImageUrl, 'http://localhost:5080', ''); UPDATE RoomPhotos SET Url = REPLACE(Url, 'http://localhost:5080', '');"

docker compose -f "$C" exec -T -u root sql rm -f /var/opt/mssql/data/restore.bak

echo "==> მზადაა. ახლა გაუშვი:  docker compose -f $C up -d --build"
