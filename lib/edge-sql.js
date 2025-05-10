var path = require('path');

exports.getCompiler = function () {
	return process.env.EDGE_SQL_NATIVE || ( process.env.EDGE_USE_CORECLR ? path.join(__dirname, 'edge-sql-coreclr.dll') : path.join(__dirname, 'edge-sql.dll'));
};
exports.getBootstrapDependencyManifest = function() {
	return path.join(__dirname, 'edge-sql-coreclr.deps.json');
}


