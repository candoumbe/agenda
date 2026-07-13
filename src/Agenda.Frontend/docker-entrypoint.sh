#!/bin/sh
set -e

# Generate runtime authentication configuration based on environment variables
cat > /usr/share/nginx/html/runtime-auth.js << 'EOF'
(function() {
  // Read environment variables with fallbacks
  const authority = '${AGENDA_AUTH_AUTHORITY:-}';
  const clientId = '${AGENDA_AUTH_CLIENT_ID:-}';
  const scope = '${AGENDA_AUTH_SCOPE:-}';

  // Build configuration object with only non-empty values
  const runtimeConfig = {};
  if (authority && authority.length > 0) {
    runtimeConfig.authority = authority;
  }
  if (clientId && clientId.length > 0) {
    runtimeConfig.clientId = clientId;
  }
  if (scope && scope.length > 0) {
    runtimeConfig.scope = scope;
  }

  // Expose configuration to global scope
  window.__agendaAuth = runtimeConfig;

  console.log('[Agenda Auth] Runtime configuration loaded', {
    hasAuthority: !!runtimeConfig.authority,
    hasClientId: !!runtimeConfig.clientId,
    hasScope: !!runtimeConfig.scope
  });
})();
EOF

# Execute the command passed to the container
exec "$@"
