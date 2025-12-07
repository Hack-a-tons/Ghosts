#!/usr/bin/env bash
set -eu

cd "$(dirname "$0")"

show_help() {
    cat << EOF
Deploy GhostLayer API to remote server

Usage: $(basename "$0") [options]

Options:
  -m MESSAGE    Commit and push changes before deploying
  -h, --help    Show this help

Examples:
  $(basename "$0")              # Deploy current remote state
  $(basename "$0") -m "fix"     # Commit, push, then deploy
EOF
    exit 0
}

COMMIT_MSG=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help) show_help ;;
        -m) COMMIT_MSG="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

source .env

if [[ -n "$COMMIT_MSG" ]]; then
    echo "Committing and pushing..."
    cd ..
    git add -A
    git commit -m "$COMMIT_MSG"
    git push
    cd backend
fi

echo "Deploying to $DEPLOY_HOST:$DEPLOY_PATH..."
scp .env "$DEPLOY_HOST:$DEPLOY_PATH/backend/"
ssh "$DEPLOY_HOST" "cd $DEPLOY_PATH/backend && git pull && docker compose build && docker compose up -d"
echo "✓ Deployed"
