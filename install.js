const path = require('path')
const fs = require('fs');

if (process.platform === 'win32'){
    if(process.arch === 'x64'){
        copy('win-x64', true);
    }
    else if(process.arch === 'arm64'){
        copy('win-arm64', true);
    }
    else if(process.arch === 'ia32'){
        copy('win-x86', true);
    }
    else{
        archError();
    }
}
else if(process.platform === 'darwin'){
    copy('darwin');
}
else if(process.platform === 'linux'){
    copy('linux');
}
else{
    throw new Error(`Platform '${process.platform}' is not supported. `);
}

function archError(){
    throw new Error(`Process architecture '${process.arch}' is not supported on '${process.platform}'`);
}

function copy(source, sni) {

    fs.copyFileSync(path.resolve(__dirname, `lib/${source}/System.Data.SqlClient.dll`), path.resolve(__dirname, `lib/System.Data.SqlClient.dll`))
    if(sni){
        fs.copyFileSync(path.resolve(__dirname, `lib/${source}/sni.dll`), path.resolve(__dirname, `lib/sni.dll`))
    }
}