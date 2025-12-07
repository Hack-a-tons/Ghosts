#!/usr/bin/env bash
set -eu

cd "$(dirname "$0")"

DEPLOY_HOST="ghosts.api.app.hurated.com"
DEPLOY_PATH="/home/dbystruev/Ghosts"

show_help() {
    cat << EOF
Deploy GhostLayer to remote server

Usage: $(basename "$0") <target> [options]

Targets:
  backend       Deploy backend API only
  unity         Deploy Unity build only
  all           Deploy both backend and unity

Options:
  -m MESSAGE    Commit and push changes before deploying
  -h, --help    Show this help

Examples:
  $(basename "$0") backend              # Deploy backend
  $(basename "$0") all -m "fix"         # Commit, push, deploy all
EOF
    exit 0
}

TARGET=""
COMMIT_MSG=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help) show_help ;;
        -m) COMMIT_MSG="$2"; shift 2 ;;
        backend|unity|all) TARGET="$1"; shift ;;
        *) echo "Unknown option: $1"; show_help ;;
    esac
done

[[ -z "$TARGET" ]] && { echo "Error: target required"; show_help; }

if [[ -n "$COMMIT_MSG" ]]; then
    echo "Committing and pushing..."
    git add -A
    git commit -m "$COMMIT_MSG"
    git push
fi

deploy_backend() {
    echo "Deploying backend to $DEPLOY_HOST:$DEPLOY_PATH/backend..."
    scp backend/.env "$DEPLOY_HOST:$DEPLOY_PATH/backend/"
    ssh "$DEPLOY_HOST" "cd $DEPLOY_PATH && git pull && cd backend && docker compose build && docker compose up -d"
    echo "✓ Backend deployed"
}

deploy_unity() {
    echo "Deploying Unity to $DEPLOY_HOST:$DEPLOY_PATH/unity..."
    # Unity builds are typically large, only sync necessary files
    ssh "$DEPLOY_HOST" "cd $DEPLOY_PATH && git pull"
    echo "✓ Unity deployed"
}

case $TARGET in
    backend) deploy_backend ;;
    unity) deploy_unity ;;
    all) deploy_backend; deploy_unity ;;
esac
