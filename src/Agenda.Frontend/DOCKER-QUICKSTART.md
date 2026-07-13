# 🚀 Docker Frontend - Quick Start Guide

## 📋 Created Files

```
src/Agenda.Frontend/
├── Dockerfile                 # Secure multi-stage build
├── docker-entrypoint.sh       # Runtime environment variable injection script
├── nginx.conf                 # Optimized Nginx configuration
├── default.conf               # Nginx vhost with CSP and security headers
├── .dockerignore              # Files to ignore in Docker build
├── .env.example               # Environment variables template
├── docker-compose.example.yml # Docker Compose for production
├── docker-compose.dev.yml     # Docker Compose for development
└── DOCKER.md                  # Complete documentation
```

## ⚡ Quick Start

### 1️⃣ Build the Image
```bash
cd src/Agenda.Frontend
docker build -t agenda-frontend:latest .
```

### 2️⃣ Run Container with Environment Variables
```bash
docker run -p 8080:8080 \
  -e AGENDA_AUTH_AUTHORITY="https://keycloak.example.com/auth/realms/agenda" \
  -e AGENDA_AUTH_CLIENT_ID="agenda-frontend" \
  -e AGENDA_AUTH_SCOPE="openid profile email" \
  agenda-frontend:latest
```

### 3️⃣ Access the Application
```
http://localhost:8080
```

### 4️⃣ Verify Authentication Configuration
```bash
curl http://localhost:8080/public/runtime-auth.js
```

## 🐳 With Docker Compose

### Production
```bash
# Copy and adapt the file
cp docker-compose.example.yml docker-compose.yml

# Edit file with your variables
nano docker-compose.yml

# Launch
docker-compose up -d
```

### Development
```bash
docker-compose -f docker-compose.dev.yml up -d
```

## 🔑 Environment Variables

| Variable | Required | Example |
|----------|----------|---------|
| `AGENDA_AUTH_AUTHORITY` | No | `https://keycloak.example.com/auth/realms/agenda` |
| `AGENDA_AUTH_CLIENT_ID` | No | `agenda-frontend` |
| `AGENDA_AUTH_SCOPE` | No | `openid profile email` |

Copy `.env.example` to `.env` and adapt the values.

## ✨ Key Features

✅ **Multi-stage build** - Optimized image (only Nginx + compiled app)  
✅ **Non-root user** - Enhanced security  
✅ **Security headers** - CSP, HSTS, X-Frame-Options, etc.  
✅ **Runtime environment variables** - No need to rebuild the image  
✅ **Gzip compression** - Asset optimization  
✅ **SPA routing** - Angular Router works correctly  
✅ **Health check** - Compatible with Kubernetes/Docker Swarm  
✅ **Smart caching** - Static assets cached 1 year  

## 🔍 Useful Commands

```bash
# View logs
docker logs -f <container-id>

# Enter container
docker exec -it <container-id> sh

# Verify generated runtime-auth.js
docker exec <container-id> cat /usr/share/nginx/html/runtime-auth.js

# Health check
curl http://localhost:8080/health

# Container stats
docker stats <container-id>

# Inspect image
docker image inspect agenda-frontend:latest
```

## 📊 Image Size

```bash
docker image ls agenda-frontend

# Size depends on Nginx Alpine + compiled Angular app
# Typically: 30-50 MB
```

## 🛡️ Security

- ✅ Runs as `nginx` user (non-root)
- ✅ Sensitive files blocked (.git, node_modules, src, scripts)
- ✅ Complete security headers
- ✅ Strict Content Security Policy
- ✅ No development dependencies
- ✅ Alpine image for reduced attack surface

## 📚 See Also

- [DOCKER.md](./DOCKER.md) - Complete documentation
- [Dockerfile](./Dockerfile) - Build configuration
- [nginx.conf](./nginx.conf) - Nginx configuration
- [default.conf](./default.conf) - Vhost configuration

## ❓ Questions?

Check [DOCKER.md](./DOCKER.md#-troubleshooting) for troubleshooting.
