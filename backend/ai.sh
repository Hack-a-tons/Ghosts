#!/usr/bin/env bash

show_help() {
    cat << EOF
Test GhostLayer AI ghost creation

Usage: $(basename "$0") [options] <prompt>

Options:
  -l, --local   Use local API (default: remote)
  -h, --help    Show this help

Examples:
  $(basename "$0") "Create a friendly ghost that gives coffee coupons"
  $(basename "$0") -l "Create a spooky ghost"
EOF
    exit 0
}

LOCAL=false
PROMPT=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help) show_help ;;
        -l|--local) LOCAL=true; shift ;;
        *) PROMPT="$1"; shift ;;
    esac
done

[[ -z "$PROMPT" ]] && { echo "Error: prompt required"; show_help; }

cd "$(dirname "$0")"
source .env

if $LOCAL; then
    API_URL="http://localhost:${PORT:-6000}"
else
    API_URL="https://ghosts.api.app.hurated.com"
fi

echo "Creating ghost via $API_URL..."
curl -s -X POST "$API_URL/api/ghosts" \
    -H "Content-Type: application/json" \
    -d "{\"prompt\": \"$PROMPT\", \"lat\": 37.7749, \"lng\": -122.4194}" | jq .
