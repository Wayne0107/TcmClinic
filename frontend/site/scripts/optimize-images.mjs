// 把 assets-src/images 的原圖轉成多尺寸 WebP，輸出到 public/images，
// 並產生 shared/image-manifest.json（記錄每張圖可用寬度與原始比例，供 <ResponsiveImage> 設定 srcset 與 width/height 避免版面位移）。
// 另產生 OG 分享圖與 favicon。執行：npm run images
import { readdir, mkdir, writeFile } from 'node:fs/promises'
import { join, parse } from 'node:path'
import sharp from 'sharp'

const SRC = 'assets-src/images'
const OUT = 'public/images'
const WIDTHS = [400, 800, 1200, 1600]

await mkdir(OUT, { recursive: true })
const manifest = {}

for (const file of (await readdir(SRC)).filter((f) => /\.(jpe?g|png)$/i.test(f))) {
  const name = parse(file).name
  const input = sharp(join(SRC, file)).rotate()
  const { width, height } = await input.metadata()
  const widths = WIDTHS.filter((w) => w < width)
  if (widths.length === 0 || widths.at(-1) < width) widths.push(Math.min(width, 2000))

  for (const w of widths) {
    await input
      .clone()
      .resize({ width: w })
      .webp({ quality: 72 })
      .toFile(join(OUT, `${name}-${w}.webp`))
  }
  manifest[name] = { widths, width, height }
}

await writeFile('shared/image-manifest.json', JSON.stringify(manifest, null, 2))

// OG 分享圖：店舖招牌橫幅，裁成 1200×630
await sharp(join(SRC, 'storefront-sign-wide.jpg'))
  .resize(1200, 630, { fit: 'cover', position: 'centre' })
  .jpeg({ quality: 80, mozjpeg: true })
  .toFile('public/og-default.jpg')

// favicon：Logo 原圖白底，輸出 PNG
const logo = sharp(join(SRC, 'logo.jpg'))
for (const [size, out] of [
  [32, 'favicon-32.png'],
  [180, 'apple-touch-icon.png'],
  [192, 'icon-192.png'],
  [512, 'icon-512.png'],
]) {
  await logo
    .clone()
    .resize(size, size, { fit: 'contain', background: '#ffffff' })
    .png()
    .toFile(join('public', out))
}
await logo.clone().resize(32, 32).png().toFile('public/favicon.ico')
// 頁首 Logo（顯示 44px，2x 螢幕用 96px）
await logo.clone().resize(96, 96).webp({ quality: 85 }).toFile('public/logo-96.webp')

process.stdout.write(`已處理 ${Object.keys(manifest).length} 張圖片
`)
