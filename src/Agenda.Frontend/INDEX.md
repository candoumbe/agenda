📦 **Frontend Docker Configuration - Complete Index**

# 📑 Index of Created Files

All Docker files for the Angular frontend are in `src/Agenda.Frontend/`

## 🎯 Essential Files (Start with These 3)

### 1. 📄 [Dockerfile](./Dockerfile)
Secure multi-stage build for Angular

**Content:**
- Stage 1: Build with Node.js 22
  - `npm ci` (deterministic)
  - `npm run generate:runtime-auth` (generate auth config)
  - `npm run build` (production build)
  
- Stage 2: Production with Nginx Alpine
  - Non-root user (uid 101)
  - Security headers
  - Health check
  - 30-50 MB final

**Usage:**
```bash
docker build -t agenda-frontend:latest .
```

### 2. 🔧 [docker-entrypoint.sh](./docker-entrypoint.sh)
Entrypoint script that generates variables at runtime

**Key Feature:**
- Generates `/usr/share/nginx/html/runtime-auth.js` based on env variables
- Allows changing config without rebuild

**Used with Dockerfile:**
```bash
COPY docker-entrypoint.sh /
ENTRYPOINT ["/docker-entrypoint.sh"]
```

### 3. ⚙️ [nginx.conf](./nginx.conf) + [default.conf](./default.conf)
Optimized and secure Nginx configuration

**nginx.conf:**
- Worker processes auto-scaling
- Gzip compression
- Optimized logging
- Generic security headers

**default.conf:**
- Main vhost (port 8080)
- SPA routing (all routes → index.html)
- Specific security headers (CSP, HSTS)
- Smart caching (assets 1 year, HTML no cache)
- Blocking sensitive files
- Health check endpoint

---

## 📋 Configuration Files

### 🌍 [.env.example](./.env.example)
Environment variables template

**Available Variables:**
```bash
AGENDA_AUTH_AUTHORITY=https://keycloak.example.com/auth/realms/agenda
AGENDA_AUTH_CLIENT_ID=agenda-frontend
AGENDA_AUTH_SCOPE=openid profile email
```

**Usage:**
```bash
cp .env.example .env
# Edit .env with your values
docker run --env-file .env -p 8080:8080 agenda-frontend:latest
```

### 🐳 [docker-compose.example.yml](./docker-compose.example.yml)
Docker Compose for production

**Includes:**
- Frontend service
- Port mapping
- Environment variables
- Health checks
- Network

**Usage:**
```bash
cp docker-compose.example.yml docker-compose.yml
docker-compose up -d
```

### 🛠️ [docker-compose.dev.yml](./docker-compose.dev.yml)
Docker Compose for development

**Differences:**
- Different container name (*-dev)
- Port 8080 (same as prod for testing)
- Logs enabled for debugging
- Optional volumes for live reload

**Usage:**
```bash
docker-compose -f docker-compose.dev.yml up
```

### 🚫 [.dockerignore](./.dockerignore)
Files to ignore in Docker build

**Contains:**
- node_modules, dist, coverage
- TypeScript sources (.ts)
- Tests (*.spec.ts)
- Documentation
- Development configuration
- .git files, .env
- Sensitive directories

---

## 📖 Documentation

### ⚡ [DOCKER-QUICKSTART.md](./DOCKER-QUICKSTART.md)
**Get started in 5 minutes**

Sections:
- Building the image
- Running the container
- Environment variables
- Useful commands
- Quick troubleshooting

**Target Readers:** Developers who want to deploy quickly

### 📚 [DOCKER.md](./DOCKER.md)
**Complete and detailed documentation** (50+ sections)

Main Sections:
1. Architecture (multi-stage)
2. Implemented security best practices
3. Building and running the image
4. Environment variables
5. Performance optimizations
6. Verification and monitoring
7. Kubernetes deployment with complete YAML
8. Local development
9. Detailed troubleshooting
10. Custom Nginx configuration

**Target Readers:** Architects, DevOps, integrators

### 📋 [DOCKER-SUMMARY.md](./DOCKER-SUMMARY.md)
**Executive Summary**

Sections:
- Created files (recap table)
- Quick usage (3 examples)
- Implemented features
- Final structure
- Available tests
- Deployment flow
- Best practices
- Next steps

**Target Readers:** Managers, project leads, technical review

### 📄 [README.md](../../../README.md) - To Update
Add a section on frontend Docker deployment

---

## 🧪 Testing & Validation

### 🧪 [test-docker.sh](./test-docker.sh)
**Automated test script** for Dockerfile

Tests Included:
1. ✅ Health check
2. ✅ index.html access
3. ✅ runtime-auth.js file generation
4. ✅ Security headers
5. ✅ Gzip compression
6. ✅ Non-root user
7. ✅ Static asset access
8. ✅ SPA routing

**Usage:**
```bash
./test-docker.sh                    # Default image (agenda-frontend:test)
./test-docker.sh custom-image:v1.0  # Custom image
```

**Output:**
- Shows results of each test
- Displays generated runtime-auth.js content
- Shows container logs
- Return exit code 0 (success) or 1 (error)

---

## 🎯 Deployment - Specialized Files

### ☸️ [kubernetes-deployment.yaml](./kubernetes-deployment.yaml)
**Production-ready Kubernetes deployment** (200+ lines)

Includes:
- ✅ Namespace (agenda)
- ✅ ConfigMap for environment variables
- ✅ Deployment (3 replicas)
- ✅ Security Context (non-root, read-only FS, no capabilities)
- ✅ Resource requests/limits
- ✅ Liveness & Readiness probes
- ✅ EmptyDir volumes (nginx cache/run/tmp)
- ✅ Pod Anti-Affinity (spread across nodes)
- ✅ Service (ClusterIP)
- ✅ HorizontalPodAutoscaler (CPU/Memory-based)
- ✅ PodDisruptionBudget (min 1 available)
- ✅ NetworkPolicy (Ingress/Egress)
- ✅ Ingress (HTTPS with cert-manager)

**Usage:**
```bash
# Adapt the values
sed -i 's/keycloak.example.com/your-keycloak.com/g' kubernetes-deployment.yaml
sed -i 's/agenda.example.com/your-domain.com/g' kubernetes-deployment.yaml

# Apply
kubectl apply -f kubernetes-deployment.yaml

# Verify
kubectl get pods -n agenda
kubectl logs -n agenda -l app=agenda-frontend --tail=50
```

---

## 🚀 Typical Workflows

### Workflow 1: Quick Local Startup
```bash
# 1. Copy config
cp .env.example .env

# 2. Modify as needed
nano .env

# 3. Build & run
docker-compose -f docker-compose.dev.yml up

# 4. Access
open http://localhost:8080
```

### Workflow 2: Production with Docker
```bash
# 1. Build
docker build -t agenda-frontend:v1.0 .

# 2. Push
docker push registry.example.com/agenda-frontend:v1.0

# 3. Run with config
docker run \
  -e AGENDA_AUTH_AUTHORITY=https://keycloak.prod.com/... \
  -e AGENDA_AUTH_CLIENT_ID=prod-frontend \
  registry.example.com/agenda-frontend:v1.0
```

### Workflow 3: Kubernetes Deployment
```bash
# 1. Adapt config
sed -i 's/example.com/prod.domain.com/g' kubernetes-deployment.yaml

# 2. Apply
kubectl apply -f kubernetes-deployment.yaml

# 3. Verify
kubectl get pods -n agenda
kubectl port-forward -n agenda svc/agenda-frontend 8080:80
```

### Workflow 4: Test Before Production
```bash
# 1. Test the images
./test-docker.sh local-image:v1.0

# 2. Check logs
docker logs <container-id>

# 3. Validate auth config
curl http://localhost:8080/public/runtime-auth.js
```

---

## 📊 Configuration Files Comparison

| File | Environment | Replicas | User | Security |
|------|---|---|---|---|
| docker-compose.dev.yml | Dev | 1 | root* | Basic |
| docker-compose.example.yml | Prod | 1 | nginx | CSP + Headers |
| kubernetes-deployment.yaml | Prod Cloud | 3 | nginx(101) | Complete SecurityContext |

*docker-compose.dev.yml: Can run as root for easier debugging

---

## 🔒 Security: What Has Been Implemented

✅ **Non-root user** (uid 101)
✅ **Read-only filesystem** (Kubernetes)
✅ **Security headers** (CSP, HSTS, X-Frame-Options, etc.)
✅ **Sensitive files blocked** (.git, node_modules, src)
✅ **No dev dependencies** in production
✅ **Reduced capabilities** (Kubernetes: drop ALL, add none)
✅ **Network Policy** (Kubernetes: allow http, egress DNS+443 only)
✅ **No privilege escalation** (allowPrivilegeEscalation: false)

---

## 📞 Support & Resources

### Internal Documentation Files
1. [DOCKER-QUICKSTART.md](./DOCKER-QUICKSTART.md) - "How do I get started?" questions
2. [DOCKER.md](./DOCKER.md) - "How does this work?" and troubleshooting
3. [DOCKER-SUMMARY.md](./DOCKER-SUMMARY.md) - "What was created?" questions

### External Resources
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [Angular Deployment Guide](https://angular.io/guide/deployment)
- [Kubernetes Security](https://kubernetes.io/docs/concepts/security/)
- [OWASP Security Headers](https://owasp.org/www-project-secure-headers/)

---

## ✅ Checklist: Before Production

- [ ] Adapt environment variables (AGENDA_AUTH_*)
- [ ] Test locally: `./test-docker.sh`
- [ ] Build and push image to your registry
- [ ] Configure HTTPS (reverse proxy / ingress)
- [ ] Adapt kubernetes-deployment.yaml (domain, replicas, resources)
- [ ] Configure Kubernetes secrets (auth-authority, etc.)
- [ ] Test on staging
- [ ] Configure centralized logging
- [ ] Configure monitoring/alerting
- [ ] Verify backups
- [ ] Create rollback plan

---

## 🎓 Learning More

**Looking for:**
- "How do I deploy?" → [DOCKER-QUICKSTART.md](./DOCKER-QUICKSTART.md)
- "Why multi-stage?" → [DOCKER.md - Architecture](./DOCKER.md#-architecture)
- "Environment variables?" → [DOCKER.md - Environment Variables](./DOCKER.md#-environment-variables)
- "Security headers?" → [DOCKER.md - Best Practices](./DOCKER.md#-security-best-practices-implemented)
- "Kubernetes?" → [kubernetes-deployment.yaml](./kubernetes-deployment.yaml)
- "All files?" → You're reading the right file! 😉

---

**Last updated:** 2026-07-13  
**Created by:** GitHub Copilot (Squad Coordinator)  
**Version:** 1.0
