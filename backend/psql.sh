#!/usr/bin/env bash

show_help() {
    cat << EOF
Connect to GhostLayer PostgreSQL database

Usage: $(basename "$0") [options] [psql args]

Options:
  -h, --help    Show this help

Examples:
  $(basename "$0")                    # Interactive psql session
  $(basename "$0") -c "SELECT * FROM ghosts"
  echo "SELECT 1" | $(basename "$0")
EOF
    exit 0
}

[[ "${1:-}" == "-h" || "${1:-}" == "--help" ]] && show_help

cd "$(dirname "$0")"
source .env

DB_CONTAINER="${DEPLOY_PATH##*/}-db-1"
DB_USER="${POSTGRES_USER:-ghosts}"
DB_NAME="${POSTGRES_DB:-ghosts}"

if [ -t 0 ] && [ $# -eq 0 ]; then
    ssh -t "$DEPLOY_HOST" "docker exec -it $DB_CONTAINER psql -U $DB_USER -d $DB_NAME"
elif [ $# -gt 0 ]; then
    ARGS=$(printf '%q ' "$@")
    ssh "$DEPLOY_HOST" "docker exec -i $DB_CONTAINER psql -U $DB_USER -d $DB_NAME $ARGS"
else
    ssh "$DEPLOY_HOST" "docker exec -i $DB_CONTAINER psql -U $DB_USER -d $DB_NAME"
fi
