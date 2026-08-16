module.exports = {
  // Forward all requests to `http://localhost:4200/api`, to `http://localhost:5187/`
  '/api': {
    target: process.env['services__api__https__0'] || process.env['services__api__http__0'],
    pathRewrite: {
      '^/api': '',
    },
  },
};
