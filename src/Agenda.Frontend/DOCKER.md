# Frontend Docker Deployment - Guide

This guide explains how to use the Dockerfile to deploy the Angular application securely with dynamic configuration.

## 🏗️ Architecture

The Dockerfile uses a **multi-stage** approach to optimize image size:

1. **Stage 1 (Builder)**: Builds the Angular application in production mode
2. **Stage 2 (Production)**: Lightweight image with Nginx to serve the application

## 🔒 Security Best Practices Implemented

### 1. **Non-root User**
- The image runs with a dedicated Nginx user (UID 101)
- Improves security in case of container compromise

### 2. **Security Headers**
- **X-Frame-Options**: Prevents clickjacking
- **X-Content-Type-Options**: Blocks MIME sniffing
- **X-XSS-Protection**: XSS protection
- **Strict-Transport-Security**: Forces HTTPS
- **Content-Security-Policy**: Strict content policy
- **Permissions-Policy**: Disables dangerous APIs

### 3. **Nginx Secure Configuration**
- Hidden static files are blocked (`.git`, `.env`, etc.)
- Sensitive directories are inaccessible (`node_modules`, `src`, `scripts`)
- No directory listing access
- Built-in health check

### 4. **Dependency Management**
- Optimized build with `npm ci` (deterministic)
- No development dependencies in production
- Alpine image to minimize attack surface

## 🚀 Building the Image

```bash
# Build the image
docker build -t agenda-frontend:latest ./src/Agenda.Frontend

# With a specific tag
docker build -t agenda-frontend:v1.0.0 ./src/Agenda.Frontend
```

## 📦 Running the Container

### Without environment variables (minimal configuration)
```bash
docker run -p 8080:8080 agenda-frontend:latest
```

### With authentication configuration
```bash
docker run \
  -p 8080:8080 \
  -e AGENDA_AUTH_AUTHORITY="https://auth.example.com/auth/realms/agenda" \
  -e AGENDA_AUTH_CLIENT_ID="agenda-frontend" \
  -e AGENDA_AUTH_SCOPE="openid profile email" \
  agenda-frontend:latest
```

### With docker-compose
```bash
# Create a docker-compose.yml file
docker-compose up -d
```

See `docker-compose.example.yml` for a complete example.

## 🌍 Environment Variables

Environment variables are **injected at runtime** when the container starts, not during build. This allows you to modify configuration without rebuilding the image.

### Available Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `AGENDA_AUTH_AUTHORITY` | OpenID Connect / OAuth2 authority URL | `https://keycloak.example.com/auth/realms/agenda` |
| `AGENDA_AUTH_CLIENT_ID` | OIDC/OAuth2 client ID | `agenda-frontend` |
| `AGENDA_AUTH_SCOPE` | OpenID Connect scopes (space-separated) | `openid profile email` |

### Generated File Format

Environment variables are transformed into a JavaScript file injected into the HTML:

```javascript
// /public/runtime-auth.js
window.__agendaAuth = {
  "authority": "https://keycloak.example.com/auth/realms/agenda",
  "clientId": "agenda-frontend",
  "scope": "openid profile email"
};
```

This file is generated **on each container startup** based on the passed environment variables.

## 📊 Performance Optimizations

### 1. **Asset Caching**
- **Static assets** (JS, CSS, images, fonts): Cache 1 year (immutable)
- **HTML & runtime-auth.js**: No cache (refreshed on each load)
- **Gzip compression**: Enabled for all text content types

### 2. **SPA Routing**
- All routes point to `index.html`
- Angular Router handles client-side navigation
- 404 error redirects to the application

### 3. **Health Check**
- `/health` endpoint available for orchestrators (Kubernetes, Swarm)
- Periodic checks: every 30 seconds

## 🔍 Verification and Monitoring

### Verify the container is working
```bash
# Access the application
curl -i http://localhost:8080

# Verify authentication configuration
curl http://localhost:8080/public/runtime-auth.js

# Verify health check
curl http://localhost:8080/health
```

### Container Logs
```bash
docker logs -f <container-id>
```

### Inspect the Image
```bash
docker image inspect agenda-frontend:latest
```

## 🐳 Kubernetes

### Minimal Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agenda-frontend
spec:
  replicas: 3
  selector:
    matchLabels:
      app: agenda-frontend
  template:
    metadata:
      labels:
        app: agenda-frontend
    spec:
      containers:
      - name: frontend
        image: agenda-frontend:latest
        ports:
        - containerPort: 8080
        env:
        - name: AGENDA_AUTH_AUTHORITY
          valueFrom:
            configMapKeyRef:
              name: agenda-config
              key: auth-authority
        - name: AGENDA_AUTH_CLIENT_ID
          valueFrom:
            configMapKeyRef:
              name: agenda-config
              key: auth-client-id
        - name: AGENDA_AUTH_SCOPE
          valueFrom:
            configMapKeyRef:
              name: agenda-config
              key: auth-scope
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "500m"
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: agenda-config
data:
  auth-authority: "https://keycloak.example.com/auth/realms/agenda"
  auth-client-id: "agenda-frontend"
  auth-scope: "openid profile email"
```

### Kubernetes Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: agenda-frontend
spec:
  type: LoadBalancer
  selector:
    app: agenda-frontend
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
```

## 📝 Local Development

During development, you can also build and test locally:

```bash
# Build the image
docker build -t agenda-frontend:dev ./src/Agenda.Frontend

# Run with environment variables from a .env file
docker run --env-file .env -p 8080:8080 agenda-frontend:dev

# Use docker-compose for a complete environment
docker-compose -f docker-compose.dev.yml up
```

## 🐛 Troubleshooting

### Container starts but page won't load
- Check logs: `docker logs <container-id>`
- Ensure port 8080 is not blocked

### "Permission denied" error at startup
- Container runs as `nginx` user
- Ensure file permissions are correct

### Environment variables not applied
- Variables must be passed with `-e` flag or via `--env-file`
- Ensure they are set BEFORE container startup
- Verify generated file: `docker exec <container-id> cat /usr/share/nginx/html/runtime-auth.js`

### Secrets management in production
```bash
# NEVER pass secrets in plain text via -e
# Use Docker or Kubernetes secrets instead

# Docker Swarm
docker secret create agenda-auth-authority -
docker service create \
  --secret agenda-auth-authority \
  -e AGENDA_AUTH_AUTHORITY_FILE=/run/secrets/agenda-auth-authority \
  ...

# Kubernetes
kubectl create secret generic agenda-secrets \
  --from-literal=auth-authority=https://...
```

## ⚙️ Custom Nginx Configuration

If you need to modify the Nginx configuration:

1. Edit the `default.conf` file
2. Rebuild the image: `docker build -t agenda-frontend:latest .`

Configuration files are copied into the Dockerfile.

## 📚 Resources

- [Nginx Documentation](https://nginx.org/en/docs/)
- [Security Headers](https://securityheaders.com/)
- [Angular Deployment](https://angular.io/guide/deployment)
- [OWASP Security Headers](https://owasp.org/www-project-secure-headers/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## ✅ Production Checklist

- [ ] Environment variables properly configured
- [ ] HTTPS enabled (reverse proxy / load balancer)
- [ ] Health checks in place in the orchestrator
- [ ] Centralized logging configured
- [ ] Resource limits defined
- [ ] Backup/rollback strategy in place
- [ ] Monitoring and alerting configured
- [ ] Docker image security scan completed
