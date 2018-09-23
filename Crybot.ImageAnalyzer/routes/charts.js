var express = require('express');
var router  = express.Router();
var path    = require('path');
const puppeteer = require('puppeteer');
var azure = require('azure-storage');
function timeout(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}
/* GET charts listing. */
router.get('/', function(req, res, next) {

  const market = req.query.market;
  const timeoutPeriod = req.query.timeout;
  console.log(`started ${market} request at ${Date.now}`);
  console.log('req.chartUrl: ' + req.query.chartUrl);
  console.log('req.market: ' + req.query.market);
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
      javascriptEnabled: true,
      phantomConfig: { "ssl-protocol":"ANY", 'ignore-ssl-errors': 'true' }
    };
    const browser = await puppeteer.launch(options);
    const page = await browser.newPage();
    await page.goto(req.query.chartUrl);
    await timeout(timeoutPeriod);
    const tempImage = `${market}-${req.query.id}.png`; 
    await page.screenshot({path: path.join(__dirname, tempImage)});
    console.log(`finished ${market} request at ${Date.now}`);
    var blobService = azure.createBlobService();
    blobService.createContainerIfNotExists('trading-images', {
      publicAccessLevel: 'blob'
    }, function(error, result, response) {
      if (!error) {
        blobService.createBlockBlobFromLocalFile('trading-images', tempImage, path.join(__dirname, tempImage), function(error, result, response) {
          if (!error) {
            console.log(`file ${tempImage} uploaded to azure storage`)
          }
        else console.log(error);
      });
      }
      else console.log(error);
    });
    res.sendFile(path.join(__dirname, tempImage));
  })();
});

module.exports = router;
