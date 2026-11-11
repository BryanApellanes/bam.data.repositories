#!/usr/bin/env bash
# Starts the MongoDB container the MongoRepository integration tests run against
# (bam.data.repositories.tests, --it). Mirrors bam.data's start-test-containers.sh.
set -euo pipefail

NAME="bam-data-test-mongo"
IMAGE="docker.io/library/mongo:8.0"
PORT=27017
TIMEOUT=60

if podman inspect --format '{{.State.Running}}' "$NAME" 2>/dev/null | grep -q true; then
    echo "[$NAME] already running"
    exit 0
fi

echo "[$NAME] starting..."
podman rm -f "$NAME" 2>/dev/null || true
podman run -d --name "$NAME" -p "127.0.0.1:$PORT:27017" "$IMAGE"

elapsed=0
until podman exec "$NAME" mongosh --quiet --eval "db.runCommand({ ping: 1 }).ok" 2>/dev/null | grep -q 1; do
    sleep 2
    elapsed=$((elapsed + 2))
    if [ "$elapsed" -ge "$TIMEOUT" ]; then
        echo "[$NAME] timed out waiting for mongod" >&2
        exit 1
    fi
done
echo "[$NAME] ready on localhost:$PORT"
