// ბაზაში დარჩენილი (Cloudflare r2.dev და სხვა) სურათების Cloudinary-ზე გადატანა.
// Cloudinary თვითონ ჩამოტვირთავს თითო URL-ს და შეინახავს. ფაილები შენს კომპიუტერზე არ გადმოდის.
//
// 1) SSMS-ში გაუშვი (HotelsManagementSystemDB-ზე) და შედეგი შეინახე CSV-ად:
//    deploy\cloudinary\remaining-urls.csv   (Results -> მარჯვენა ღილაკი -> Save Results As...)
//      SELECT DISTINCT url FROM (
//        SELECT ImageUrl AS url FROM Hotels     WHERE ImageUrl NOT LIKE N'https://res.cloudinary.com/%'
//        UNION
//        SELECT Url             FROM RoomPhotos WHERE Url      NOT LIKE N'https://res.cloudinary.com/%'
//      ) x;
// 2) PowerShell (HMS საქაღალდიდან):
//      $env:CLOUDINARY_URL='cloudinary://<API_KEY>:<API_SECRET>@<CLOUD_NAME>'
//      node deploy\cloudinary\upload-remote.mjs          (ან --dry სატესტოდ)
// 3) შედეგი: url-map-remote.json და update-urls-remote.sql -> გაუშვი SSMS-ში.

import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const DRY = process.argv.includes('--dry');
const here = path.dirname(fileURLToPath(import.meta.url));
const csvPath = process.argv.find((a) => a.endsWith('.csv')) ?? path.join(here, 'remaining-urls.csv');

const match = process.env.CLOUDINARY_URL?.match(/^cloudinary:\/\/([^:]+):([^@]+)@(.+)$/);
if (!match) {
  console.error('დააყენე CLOUDINARY_URL=cloudinary://<API_KEY>:<API_SECRET>@<CLOUD_NAME>');
  process.exit(1);
}
const [, apiKey, apiSecret, cloud] = match;
const deliveryBase = `https://res.cloudinary.com/${cloud}/image/upload/f_auto,q_auto`;

// URL-ების წაკითხვა CSV-დან (BOM, ბრჭყალები, სათაური იგნორდება)
const raw = (await readFile(csvPath)).toString('utf8').replace(/^﻿/, '');
const urls = [
  ...new Set(
    raw
      .split(/\r?\n/)
      .map((l) => l.trim().replace(/^"|"$/g, ''))
      .filter((l) => /^https?:\/\//i.test(l))
  )
].filter((u) => !u.startsWith('https://res.cloudinary.com/'));

if (!urls.length) {
  console.error(`URL-ები ვერ ვიპოვე ფაილში: ${csvPath}`);
  process.exit(1);
}

// public_id: ფაილის სახელი, უსაფრთხო სიმბოლოებით; დუბლიკატზე მოკლე hash
const used = new Set();
const jobs = urls.map((src) => {
  const pathname = new URL(src).pathname;
  let name = path.parse(decodeURIComponent(pathname)).name.replace(/[^A-Za-z0-9_-]/g, '_').slice(0, 80) || 'img';
  if (used.has(name)) name += '-' + createHash('sha1').update(src).digest('hex').slice(0, 6);
  used.add(name);
  const publicId = `hms/remote/${name}`;
  return { old: src, src, publicId, url: `${deliveryBase}/${publicId}` };
});

console.log(`ატვირთვაა: ${jobs.length} URL -> cloud "${cloud}"${DRY ? '  (DRY RUN)' : ''}`);

async function upload(src, publicId, attempt = 1) {
  const timestamp = String(Math.floor(Date.now() / 1000));
  const params = { overwrite: 'true', public_id: publicId, timestamp };
  const toSign =
    Object.keys(params)
      .sort()
      .map((k) => `${k}=${params[k]}`)
      .join('&') + apiSecret;
  const form = new FormData();
  for (const [k, v] of Object.entries(params)) form.append(k, v);
  form.append('api_key', apiKey);
  form.append('signature', createHash('sha1').update(toSign).digest('hex'));
  form.append('file', src);
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
    if (attempt < 3) {
      await new Promise((r) => setTimeout(r, 1500 * attempt));
      return upload(src, publicId, attempt + 1);
    }
    throw err;
  }
}

let done = 0;
let aborted = false;
const failed = [];
const errors = new Map();
if (!DRY) {
  const queue = [...jobs];
  await Promise.all(
    Array.from({ length: 4 }, async () => {
      while (queue.length && !aborted) {
        const job = queue.shift();
        try {
          await upload(job.src, job.publicId);
          console.log(`[${++done}/${jobs.length}] ${job.publicId}`);
        } catch (err) {
          failed.push(job);
          errors.set(err.message, (errors.get(err.message) ?? 0) + 1);
          console.error(`შეცდომა: ${job.src}: ${err.message}`);
          if (done === 0 && failed.length >= 3) aborted = true;
        }
      }
    })
  );
  if (errors.size) {
    console.error('\nშეცდომების მიზეზები:');
    for (const [msg, n] of errors) console.error(`  ${n} x ${msg}`);
  }
  if (done === 0) {
    console.error('\nარაფერი აიტვირთა, ფაილები არ შეიქმნა.');
    process.exit(2);
  }
}

const ok = DRY ? jobs : jobs.filter((j) => !failed.includes(j));
await writeFile(
  path.join(here, 'url-map-remote.json'),
  JSON.stringify(Object.fromEntries(ok.map((j) => [j.old, j.url])), null, 2)
);

const esc = (s) => s.replace(/'/g, "''");
const rows = ok.map((j) => `(N'${esc(j.old)}', N'${esc(j.url)}')`);
const sql = `-- გაუშვი HotelsManagementSystemDB-ზე. ჯერ გააკეთე backup!
USE [HotelsManagementSystemDB];
GO
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @m TABLE (oldUrl nvarchar(600), newUrl nvarchar(600));
INSERT INTO @m (oldUrl, newUrl) VALUES
${rows.join(',\n')};

UPDATE h SET h.ImageUrl = m.newUrl FROM Hotels h JOIN @m m ON h.ImageUrl = m.oldUrl;
PRINT CONCAT('Hotels განახლდა: ', @@ROWCOUNT);

UPDATE p SET p.Url = m.newUrl FROM RoomPhotos p JOIN @m m ON p.Url = m.oldUrl;
PRINT CONCAT('RoomPhotos განახლდა: ', @@ROWCOUNT);

COMMIT;

-- დარჩენილი (არა-Cloudinary) მისამართები; იდეალურად 0:
SELECT 'Hotels' AS tbl, Id, ImageUrl AS url FROM Hotels WHERE ImageUrl NOT LIKE N'https://res.cloudinary.com/%'
UNION ALL
SELECT 'RoomPhotos', Id, Url FROM RoomPhotos WHERE Url NOT LIKE N'https://res.cloudinary.com/%';
`;
await writeFile(path.join(here, 'update-urls-remote.sql'), sql);

console.log(
  `\nმზადაა: ${ok.length} წარმატებული, ${failed.length} შეცდომა.\n` +
    `ფაილები: deploy/cloudinary/url-map-remote.json და update-urls-remote.sql`
);
if (failed.length) process.exit(2);
