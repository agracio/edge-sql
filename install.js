const path = require('path')
const fs = require('fs');

let sni = process.platform === 'win32';
copy(process.platform, sni);

function copy(source, sni) {

    fs.copyFileSync(path.resolve(__dirname, `lib/${source}/System.Data.SqlClient.dll`), path.resolve(__dirname, `lib/System.Data.SqlClient.dll`))
    if(sni){
        fs.copyFileSync(path.resolve(__dirname, `lib/${source}/sni.dll`), path.resolve(__dirname, `lib/sni.dll`))
    }
}