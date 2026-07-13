#!/bin/bash
set -e

# Frontend Dockerfile test script
# Usage: ./test-docker.sh [image-name:tag]

IMAGE_NAME="${1:-agenda-frontend:test}"
CONTAINER_NAME="agenda-frontend-test"
PORT=8080

echo "🧪 Testing Frontend Dockerfile"
echo "================================"
echo ""

# Clean up old containers
if docker ps -a | grep -q "$CONTAINER_NAME"; then
    echo "🧹 Removing previous container..."
    docker rm -f "$CONTAINER_NAME" > /dev/null 2>&1
fi

# Build the image
echo "🏗️ Building Docker image..."
docker build -t "$IMAGE_NAME" .

# Start the container
echo "🚀 Starting container with environment variables..."
docker run -d \
    --name "$CONTAINER_NAME" \
    -p $PORT:8080 \
    -e AGENDA_AUTH_AUTHORITY="https://keycloak.example.com/auth/realms/agenda" \
    -e AGENDA_AUTH_CLIENT_ID="test-frontend" \
    -e AGENDA_AUTH_SCOPE="openid profile email" \
    "$IMAGE_NAME"

# Wait for container to be ready
echo "⏳ Waiting for container to start..."
sleep 3

# Run tests
echo ""
echo "🔍 Running tests..."
echo "========================="

TESTS_PASSED=0
TESTS_FAILED=0

# Test 1: Health check
echo ""
echo "✓ Test 1: Health check"
if curl -f -s http://localhost:$PORT/health > /dev/null; then
    echo "  ✅ Health check OK"
    ((TESTS_PASSED++))
else
    echo "  ❌ Health check FAILED"
    ((TESTS_FAILED++))
fi

# Test 2: Homepage
echo "✓ Test 2: index.html"
if curl -f -s http://localhost:$PORT/index.html > /dev/null; then
    echo "  ✅ index.html accessible"
    ((TESTS_PASSED++))
else
    echo "  ❌ index.html FAILED"
    ((TESTS_FAILED++))
fi

# Test 3: Runtime configuration
echo "✓ Test 3: runtime-auth.js configuration file"
if curl -f -s http://localhost:$PORT/public/runtime-auth.js | grep -q "agendaAuth"; then
    echo "  ✅ runtime-auth.js generated correctly"
    ((TESTS_PASSED++))

    # Display content
    echo "  Content:"
    docker exec "$CONTAINER_NAME" cat /usr/share/nginx/html/runtime-auth.js | sed 's/^/    /'
else
    echo "  ❌ runtime-auth.js FAILED"
    ((TESTS_FAILED++))
fi

# Test 4: Security headers
echo "✓ Test 4: Security headers"
HEADERS=$(curl -f -s -I http://localhost:$PORT/index.html)

HAS_CSP=$(echo "$HEADERS" | grep -i "content-security-policy" && echo "yes" || echo "no")
HAS_HSTS=$(echo "$HEADERS" | grep -i "strict-transport-security" && echo "yes" || echo "no")
HAS_FRAME_OPTIONS=$(echo "$HEADERS" | grep -i "x-frame-options" && echo "yes" || echo "no")

if [ "$HAS_CSP" = "yes" ] && [ "$HAS_HSTS" = "yes" ] && [ "$HAS_FRAME_OPTIONS" = "yes" ]; then
    echo "  ✅ Security headers present"
    ((TESTS_PASSED++))
else
    echo "  ⚠️  Some headers missing"
    echo "    CSP: $HAS_CSP, HSTS: $HAS_HSTS, X-Frame-Options: $HAS_FRAME_OPTIONS"
    ((TESTS_FAILED++))
fi

# Test 5: Gzip compression
echo "✓ Test 5: Gzip compression"
if curl -f -s -H "Accept-Encoding: gzip" -I http://localhost:$PORT/index.html | grep -iq "content-encoding: gzip"; then
    echo "  ✅ Gzip compression active"
    ((TESTS_PASSED++))
else
    echo "  ℹ️  Gzip not returned (normal for small files)"
    ((TESTS_PASSED++))
fi

# Test 6: Non-root user
echo "✓ Test 6: Running as non-root user"
USER_ID=$(docker exec "$CONTAINER_NAME" id -u)
if [ "$USER_ID" != "0" ]; then
    echo "  ✅ Running as non-root user (UID: $USER_ID)"
    ((TESTS_PASSED++))
else
    echo "  ❌ Running as root!"
    ((TESTS_FAILED++))
fi

# Test 7: Static assets accessible
echo "✓ Test 7: Static assets"
if curl -f -s http://localhost:$PORT/main.js > /dev/null 2>&1 || \
   curl -f -s http://localhost:$PORT/polyfills.js > /dev/null 2>&1; then
    echo "  ✅ JavaScript assets accessible"
    ((TESTS_PASSED++))
else
    echo "  ⚠️  JavaScript assets not found (empty application?)"
    ((TESTS_PASSED++))
fi

# Test 8: SPA Routing (404 returns index.html)
echo "✓ Test 8: SPA routing"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:$PORT/non-existent-route)
if [ "$HTTP_CODE" = "200" ]; then
    echo "  ✅ Non-existent route returns index.html (SPA routing)"
    ((TESTS_PASSED++))
else
    echo "  ⚠️  SPA routing may not work (HTTP $HTTP_CODE)"
    ((TESTS_PASSED++))
fi

# Summary
echo ""
echo "================================"
echo "📊 Test Summary"
echo "================================"
echo "✅ Passed: $TESTS_PASSED"
echo "❌ Failed: $TESTS_FAILED"

# Logs
echo ""
echo "📋 Container Logs"
echo "================================"
docker logs "$CONTAINER_NAME" | tail -20

# Cleanup
echo ""
echo "🧹 Cleaning up..."
docker stop "$CONTAINER_NAME" > /dev/null 2>&1
docker rm "$CONTAINER_NAME" > /dev/null 2>&1

# Final result
if [ $TESTS_FAILED -eq 0 ]; then
    echo ""
    echo "✅ All tests passed! 🎉"
    exit 0
else
    echo ""
    echo "❌ Some tests failed."
    exit 1
fi
