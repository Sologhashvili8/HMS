// HMS ფოტოების ატვირთვა Cloudinary-ზე + ბაზის განახლების SQL-ის გენერაცია.
//
// გამოყენება (PowerShell, HMS საქაღალდიდან; Node 18+):
//   $env:CLOUDINARY_URL="cloudinary://<API_KEY>:<API_SECRET>@<CLOUD_NAME>"
//   node deploy\cloudinary\upload.mjs            # ატვირთვა
//   node deploy\cloudinary\upload.mjs --dry      # მხოლოდ სია და SQL, ატვირთვის გარეშე
//
// შედეგი (deploy/cloudinary/ საქაღალდეში):
//   url-map.json      ძველი მისამართი -> ახალი Cloudinary მისამართი
//   update-urls.sql   გაუშვი HotelsManagementSystemDB-ზე, რომ Hotels/RoomPhotos განახლდეს
//
// API_SECRET არსად ინახება, მხოლოდ ამ PowerShell სესიის გარემოშია.

import { createHash } from 'node:crypto';
import { readdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const DRY = process.argv.includes('--dry');
const here = path.dirname(fileURLToPath(import.meta.url));
const imagesDir = process.env.IMAGES_DIR ?? path.resolve(here, '../../src/HMS.API/wwwroot/images');

// მთავარი გვერდის ქალაქების სურათები (ახლა Cloudflare r2.dev-ზეა)
const CITY_IMAGES = {
  Kachreti: 'https://pub-47ee3f9d203f4b9abc14f462df87d066.r2.dev/images.jpg',
  Tbilisi: 'https://pub-5ce6bd5f61e640a4b3d6c89a9620874a.r2.dev/narikala-fortress.jpg',
  Batumi:
    'https://pub-c8d4fc39cf814f659204279b15fab85c.r2.dev/9931fb5d244dedcfd8b162193837a9e57ae24bf25ee49938467d1950935ad380.avif',
  Stepantsminda: 'https://pub-8aef0bf37b6e452fa764c29fcf5f673e.r2.dev/imagesss.jpg',
  Telavi: 'https://pub-52840b237afe4078a672511a7af6782a.r2.dev/5214942020_7e358e4343.jpg'
};

const match = process.env.CLOUDINARY_URL?.match(/^cloudinary:\/\/([^:]+):([^@]+)@(.+)$/);
if (!match) {
  console.error('დააყენე CLOUDINARY_URL=cloudinary://<API_KEY>:<API_SECRET>@<CLOUD_NAME>');
  process.exit(1);
}
const [, apiKey, apiSecret, cloud] = match;
const deliveryBase = `https://res.cloudinary.com/${cloud}/image/upload/f_auto,q_auto`;

async function upload(file, publicId, attempt = 1) {
  const timestamp = String(Math.floor(Date.now() / 1000));
  const params = { overwrite: 'true', public_id: publicId, timestamp };
  const toSign =
    Object.keys(params)
      .sort()
      .map((k) => `${k}=${params[k]}`)
      .join('&') + apiSecret;
  const signature = createHash('sha1').update(toSign).digest('hex');

  const form = new FormData();
  for (const [k, v] of Object.entries(params)) form.append(k, v);
  form.append('api_key', apiKey);
  form.append('signature', signature);
  if (typeof file === 'string') form.append('file', file); // დისტანციური URL
  else form.append('file', new Blob([file.buffer]), file.name);

  try {
    const res = await fetch(`https://api.cloudinary.com/v1_1/${cloud}/image/upload`, {
      method: 'POST',
      body: form
    });
    const text = await res.text();
    let json;
    try {
      json = JSON.parse(text);
    } catch {
      throw new Error(`HTTP ${res.status}: ${text.slice(0, 200)}`);
    }
    if (!res.ok) throw new Error(`HTTP ${res.status}: ${json?.error?.message ?? res.statusText}`);
    return json;
  } catch (err) {
    if (attempt < 3) return upload(file, publicId, attempt + 1);
    throw err;
  }
}

async function runPool(items, worker, size = 5) {
  const queue = [...items];
  await Promise.all(
    Array.from({ length: size }, async () => {
      while (queue.length) await worker(queue.shift());
    })
  );
}

// 1) სასტუმროს/ოთახის ფოტოები wwwroot/images-დან
const jobs = [];
for (const hotel of await readdir(imagesDir, { withFileTypes: true })) {
  if (!hotel.isDirectory()) continue;
  for (const f of await readdir(path.join(imagesDir, hotel.name))) {
    const name = path.parse(f).name;
    jobs.push({
      kind: 'local',
      old: `/images/${hotel.name}/${f}`,
      full: path.join(imagesDir, hotel.name, f),
      file: f,
      publicId: `hms/${hotel.name}/${name}`,
      url: `${deliveryBase}/hms/${hotel.name}/${name}`
    });
  }
}

// 2) ქალაქების სურათები Cloudflare-დან
for (const [city, src] of Object.entries(CITY_IMAGES)) {
  const id = `hms/cities/${city.toLowerCase()}`;
  jobs.push({ kind: 'remote', old: src, src, publicId: id, url: `${deliveryBase}/${id}` });
}

console.log(`სულ ატვირთვაა: ${jobs.length} ფაილი -> cloud "${cloud}"${DRY ? '  (DRY RUN)' : ''}`);

let done = 0;
let aborted = false;
const failed = [];
const errors = new Map();
if (!DRY) {
  await runPool(jobs, async (job) => {
    if (aborted) return;
    try {
      const file =
        job.kind === 'local' ? { buffer: await readFile(job.full), name: job.file } : job.src;
      await upload(file, job.publicId);
      console.log(`[${++done}/${jobs.length}] ${job.publicId}`);
    } catch (err) {
      failed.push(job);
      errors.set(err.message, (errors.get(err.message) ?? 0) + 1);
      console.error(`შეცდომა: ${job.publicId}: ${err.message}`);
      // თუ პირველივე ფაილები ვერ იტვირთება, ეს ავტორიზაციის/კონფიგის პრობლემაა: ვჩერდებით
      if (done === 0 && failed.length >= 3) aborted = true;
    }
  });

  if (errors.size) {
    console.error('\nშეცდომების მიზეზები:');
    for (const [msg, n] of errors) console.error(`  ${n} x ${msg}`);
  }
  if (done === 0) {
    console.error('\nარაფერი აიტვირთა, ამიტომ url-map.json და update-urls.sql არ შეიქმნა.');
    process.exit(2);
  }
}

const ok = jobs.filter((j) => !failed.includes(j));
await writeFile(
  path.join(here, 'url-map.json'),
  JSON.stringify(Object.fromEntries(ok.map((j) => [j.old, j.url])), null, 2)
);

// SQL: ძველი მისამართები (/images/... ან http://localhost:5080/images/... ან r2.dev) -> Cloudinary
const esc = (s) => s.replace(/'/g, "''");
const rows = ok.map((j) => `(N'${esc(j.old)}', N'${esc(j.url)}')`);
const sql = `-- გაუშვი HotelsManagementSystemDB-ზე. ჯერ გააკეთე backup!
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @m TABLE (oldUrl nvarchar(600) PRIMARY KEY, newUrl nvarchar(600));
INSERT INTO @m (oldUrl, newUrl) VALUES
${rows.join(',\n')};

UPDATE h SET h.ImageUrl = m.newUrl
FROM Hotels h JOIN @m m ON h.ImageUrl IN (m.oldUrl, N'http://localhost:5080' + m.oldUrl);
PRINT CONCAT('Hotels განახლდა: ', @@ROWCOUNT);

UPDATE p SET p.Url = m.newUrl
FROM RoomPhotos p JOIN @m m ON p.Url IN (m.oldUrl, N'http://localhost:5080' + m.oldUrl);
PRINT CONCAT('RoomPhotos განახლდა: ', @@ROWCOUNT);

COMMIT;

-- დარჩენილი (არა-Cloudinary) მისამართები; იდეალურად 0:
SELECT 'Hotels' AS tbl, Id, ImageUrl AS url FROM Hotels WHERE ImageUrl NOT LIKE N'https://res.cloudinary.com/%'
UNION ALL
SELECT 'RoomPhotos', Id, Url FROM RoomPhotos WHERE Url NOT LIKE N'https://res.cloudinary.com/%';
`;
await writeFile(path.join(here, 'update-urls.sql'), sql);

console.log(
  `\nმზადაა: ${ok.length} წარმატებული, ${failed.length} შეცდომა.\n` +
    `ფაილები: deploy/cloudinary/url-map.json და update-urls.sql`
);
if (failed.length) process.exit(2);
