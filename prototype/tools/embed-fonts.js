// 把源柔黑體（Gen Jyuu Gothic）依頁面實際用到的字抽出子集，轉成 woff2，以 data URI 嵌入 HTML 尾段。
// 修改頁面文字後重跑一次，新加入的字才會用上源柔黑體。
//
// 用法（在 prototype/tools 目錄）：
//   npm install
//   node embed-fonts.js ../blush.html
//
// 字體授權：Gen Jyuu Gothic 以 SIL Open Font License 1.1 發布（http://jikasei.me/font/genjyuu/），
// 基於 Source Han Sans（Adobe）及 M+ OUTLINE FONTS。子集化屬授權允許的修改；
// 依授權，衍生字體不可使用保留字體名稱「Source」，因此 CSS 中以 "Gen Jyuu Gothic" 命名。
// 正式網站建議改為自行託管完整或常用字子集的 woff2 檔，並附上 OFL 授權文字。

const fs = require('fs');
const path = require('path');
const subsetFont = require('subset-font');

const START = '<!-- 嵌入字體：由 tools/embed-fonts.js 產生，請勿手動修改 -->';
const END = '<!-- /嵌入字體 -->';

const WEIGHTS = [
  { file: 'GenJyuuGothic-Monospace-Regular.ttf', weight: 400 },
  { file: 'GenJyuuGothic-Monospace-Bold.ttf', weight: 700 },
];

async function main() {
  const target = process.argv[2];
  if (!target) {
    console.error('請指定 HTML 檔，例如：node embed-fonts.js ../blush.html');
    process.exit(1);
  }
  let html = fs.readFileSync(target, 'utf8');
  const startAt = html.indexOf(START);
  if (startAt !== -1) {
    const endAt = html.indexOf(END, startAt);
    html = html.slice(0, startAt).trimEnd() + '\n' + html.slice(endAt + END.length).trimStart();
  }

  // 頁面所有字元 + 完整可見 ASCII + 常用全形標點
  const ascii = Array.from({ length: 95 }, (_, i) => String.fromCharCode(32 + i)).join('');
  const chars = [...new Set(html + ascii + '，。、；：？！「」『』（）《》〈〉・…—～｜．')].join('');

  const faces = [];
  for (const { file, weight } of WEIGHTS) {
    const src = fs.readFileSync(path.join(__dirname, 'fonts', file));
    const woff2 = await subsetFont(src, chars, { targetFormat: 'woff2' });
    console.log(`${file}: ${(woff2.length / 1024).toFixed(0)} KB`);
    faces.push(
      `@font-face{font-family:"Gen Jyuu Gothic";font-weight:${weight};font-style:normal;font-display:swap;` +
      `src:url(data:font/woff2;base64,${woff2.toString('base64')}) format("woff2");}`
    );
  }

  const block = `${START}\n<style>\n/* Gen Jyuu Gothic Monospace｜SIL Open Font License 1.1｜http://jikasei.me/font/genjyuu/ */\n${faces.join('\n')}\n</style>\n${END}\n`;
  fs.writeFileSync(target, html.trimEnd() + '\n\n' + block);
  console.log(`已嵌入 ${target}（${[...new Set(chars)].length} 個字元）`);
}

main().catch(err => { console.error(err); process.exit(1); });
