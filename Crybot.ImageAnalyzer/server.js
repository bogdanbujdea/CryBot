var express = require('express');
var router  = express.Router();
var path    = require('path');
const puppeteer = require('puppeteer');
var azure = require('azure-storage');
function timeout(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

  console.log('key:' + process.env.StorageAccountKey);
  console.log('name:' + process.env.StorageAccountName);
  (async () => {
    var options = {
      defaultViewport: {
        width: 1920,
        height: 1080
      },
      args: ['--no-sandbox', '--disable-setuid-sandbox'],
      shotSize: {
        width: 'all',
        height: 'all'
      },
      timeout: 3000,
      javascriptEnabled: true,
      phantomConfig: { "ssl-protocol":"ANY", 'ignore-ssl-errors': 'true' }
    };
    const browser = await puppeteer.launch(options);
    const page = await browser.newPage();
    await page.goto('https://www.tradingview.com/chart/WpYk6xkq');
    await timeout(3000);
    await page.screenshot({path: path.join(__dirname, './google.png')});
    var blobService = azure.createBlobService(process.env.StorageAccountName, process.env.StorageAccountKey);
    console.log('image downloaded, saving to blob');
    blobService.createContainerIfNotExists('trading-images', {
      publicAccessLevel: 'blob'
    }, function(error, result, response) {
      if (!error) {
        blobService.createBlockBlobFromLocalFile('trading-images', 'btcxrp.png', 'google.png', function(error, result, response) {
          if (!error) {
            console.log('file uploaded to azure storage')
          }
        else console.log(error);
      });
      }
      else console.log(error);
    });
    await browser.close();
  })();

module.exports = router;
