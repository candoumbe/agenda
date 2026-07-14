# Stage 1: Build Angular application
FROM node:24-alpine AS builder

WORKDIR /app

# Use the npm version declared by package.json
RUN corepack enable && \
    corepack prepare npm@12.0.1 --activate

# Copy package files
COPY package*.json ./

# Install dependencies with production flag to avoid dev dependencies
RUN corepack npm ci --only=production && \
    corepack npm ci

# Copy source code
COPY . .

# Generate runtime authentication configuration and build
RUN npm run generate:runtime-auth && \
    npm run build

# Stage 2: Production image with Nginx
FROM nginx:1.31-alpine

# Install curl for health checks
RUN apk add --no-cache curl

# Copy built application from builder stage
COPY --from=builder /app/dist/Agenda.Frontend/browser /usr/share/nginx/html/

# Copy nginx configuration
COPY --chown=nginx:nginx nginx.conf /etc/nginx/nginx.conf
COPY --chown=nginx:nginx default.conf /etc/nginx/conf.d/default.conf

# Copy entrypoint script
COPY --chown=nginx:nginx docker-entrypoint.sh /

RUN chmod +x /docker-entrypoint.sh

# Create directory for runtime configuration with proper permissions
RUN mkdir -p /usr/share/nginx/html/public && \
    chown -R nginx:nginx /usr/share/nginx/html

# Switch to non-root user
USER nginx

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/index.html || exit 1

# Expose port
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]

# Default command
CMD ["nginx", "-g", "daemon off;"]
