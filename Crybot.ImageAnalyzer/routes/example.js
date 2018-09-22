const puppeteer = require('puppeteer');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  await page.goto('https://www.tradingview.com/chart/WpYk6xkq');
  await page.screenshot({path: 'example.png'});

  await browser.close();
})();