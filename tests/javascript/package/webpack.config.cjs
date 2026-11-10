const path = require('path')
module.exports = {
  mode: 'production',
  target: 'web',
  entry: './web/index.js',
  output: { path: path.resolve(__dirname, 'dist'), filename: 'app.js', clean: true },
  performance: { hints: false },
}
