# HMS → DigitalOcean (Docker, ერთი სტეკი)

სამივე სერვისი (SQL + API + Frontend) ერთ Droplet-ზე, ერთი compose ფაილით. გარეთ მხოლოდ 80 პორტია.

## 1. Droplet
Ubuntu 24.04, **მინ. 4 GB RAM** (SQL Server-ს 2 GB+ უნდა). შემდეგ სერვერზე:

```bash
curl -fsSL https://get.docker.com | sh
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable
```

## 2. ბაზის backup ლოკალურიდან (შენს კომპიუტერზე)
`sql1` კონტეინერი უნდა იყოს გაშვებული. PowerShell:

```powershell
docker exec sql1 /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "<sql1-ის sa პაროლი>" -Q "BACKUP DATABASE HotelsManagementSystemDB TO DISK='/var/opt/mssql/data/hms.bak' WITH INIT"
docker cp sql1:/var/opt/mssql/data/hms.bak .\hms.bak
scp .\hms.bak root@<DROPLET_IP>:/root/hms.bak
```
თუ `mssql-tools18` არ არსებობს, გამოიყენე `/opt/mssql-tools/bin/sqlcmd` (და -C ამოიღე).

## 3. პროექტების ატვირთვა
სერვერზე ორი საქაღალდე გვერდიგვერდ: `/opt/hms/HMS` და `/opt/hms/hms-frontend`.
რეკომენდებული: `git clone` ორივე რეპოზიტორია (private GitHub). ან zip-ით, **node_modules, bin, obj გარეშე**.

## 4. .env
```bash
cd /opt/hms/HMS
cp .env.example .env && nano .env
sed -i 's/\r$//' deploy/restore-db.sh && chmod +x deploy/restore-db.sh   # Windows-იდან თუ ატვირთე
```
გასაღებების გენერაცია `.env.example`-შია. **ENCRYPTION_KEY**: თუ ლოკალურ ბაზაში დაშიფრული Payments გაქვს და გინდა გახსნა, ლოკალური იგივე გასაღები ჩაწერე.

## 5. ბაზის აღდგენა და გაშვება
```bash
./deploy/restore-db.sh /root/hms.bak
docker compose -f docker-compose.prod.yml up -d --build
```
გახსენი `http://<DROPLET_IP>`. ახალი ბაზა არ იქმნება, მუშაობს შენი აღდგენილი.
თუ restore-ზე ვერსიის შეცდომაა (backup უფრო ახალი SQL-იდანაა), compose-ში `sql` სერვისის image შეცვალე ლოკალური `sql1`-ის ვერსიით.

## განახლება
```bash
cd /opt/hms/HMS && git pull && (cd ../hms-frontend && git pull)
docker compose -f docker-compose.prod.yml up -d --build
```

## სასარგებლო
```bash
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f hms-api
```

## მერე (რეკომენდებული)
- დომენი + HTTPS (Caddy ან nginx + Let's Encrypt).
- რეგულარული backup: `docker compose exec sql ... BACKUP DATABASE ...` cron-ით + DO Snapshots.
