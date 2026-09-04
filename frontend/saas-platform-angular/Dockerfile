# ==============================================================================
# Multi-stage Dockerfile for SaaS Organization Platform Angular Frontend
# ==============================================================================

# Stage 1: Build Angular application
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies first for Docker layer caching
COPY package*.json ./
RUN npm ci

# Copy application source code
COPY . .

# Build production bundle
RUN npm run build -- --configuration=production

# Stage 2: Lightweight Production Web Server
FROM nginx:alpine AS final

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy compiled browser assets from build stage
COPY --from=build /app/dist/saas-platform-angular/browser /usr/share/nginx/html

# Expose HTTP port
EXPOSE 80

# Start Nginx in foreground
CMD ["nginx", "-g", "daemon off;"]
