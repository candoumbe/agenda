const fs = require('fs');
const path = require('path');

const outputPath = path.resolve(__dirname, '../public/runtime-auth.js');

function readRuntimeValue(...keys) {
  for (const key of keys) {
    const value = process.env[key];
    if (typeof value === 'string' && value.trim().length > 0) {
      return value.trim();
    }
  }

  return undefined;
}

const runtimeConfig = {
  authority: readRuntimeValue('AGENDA_AUTH_AUTHORITY', 'Authority'),
  clientId: readRuntimeValue('AGENDA_AUTH_CLIENT_ID', 'clientId'),
  scope: readRuntimeValue('AGENDA_AUTH_SCOPE', 'scope')
};

const sanitizedConfig = Object.fromEntries(
  Object.entries(runtimeConfig).filter(([, value]) => typeof value === 'string' && value.length > 0)
);

const runtimeContent = `window.__agendaAuth = ${JSON.stringify(sanitizedConfig, null, 2)};\n`;

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, runtimeContent, 'utf8');
