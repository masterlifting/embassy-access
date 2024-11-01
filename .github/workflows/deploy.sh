#!/bin/bash

set -e

cd /app/src/embassy-access

git stash
git pull origin main --recurse-submodules

chmod 600 .docker-compose/data/

docker-compose -p embassy-access -f .docker-compose/docker-compose.yaml down
docker-compose -p embassy-access -f .docker-compose/docker-compose.yaml up -d --build
docker system prune -af --volumes