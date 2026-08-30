#!/usr/bin/env bash
# If invoked via sh (not bash), re-exec with bash
if [ -z "${BASH_VERSION:-}" ]; then
  exec bash "$0" "$@"
fi
set -eu

BLUE='\033[0;34m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'

echo -e "${BLUE}"
echo "  ╔══════════════════════════════════════╗"
echo "  ║     Cyprus Invoice Fixer Setup       ║"
echo "  ╚══════════════════════════════════════╝"
echo -e "${NC}"

# ── Detect OS ────────────────────────────────────────────────
OS="unknown"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
  if grep -qi microsoft /proc/version 2>/dev/null; then OS="wsl"
  else OS="linux"; fi
elif [[ "$OSTYPE" == "darwin"* ]]; then OS="mac"
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then OS="windows"
fi
echo -e "  Detected OS: ${YELLOW}${OS}${NC}"

# ── Helper: install package ───────────────────────────────────
install_pkg() {
  if command -v apt-get &>/dev/null; then
    sudo apt-get update -qq && sudo apt-get install -y -qq "$@"
  elif command -v dnf &>/dev/null; then
    sudo dnf install -y -q "$@"
  elif command -v yum &>/dev/null; then
    sudo yum install -y -q "$@"
  elif command -v pacman &>/dev/null; then
    sudo pacman -Sy --noconfirm "$@"
  elif command -v brew &>/dev/null; then
    brew install "$@"
  else
    echo -e "${RED}✗ Cannot auto-install packages. Please install manually: $*${NC}"
    exit 1
  fi
}

# ── Check & install Git ───────────────────────────────────────
if ! command -v git &>/dev/null; then
  echo -e "${YELLOW}⚙  Git not found. Installing...${NC}"
  if [[ "$OS" == "mac" ]]; then
    xcode-select --install 2>/dev/null || brew install git
  else
    install_pkg git
  fi
  echo -e "${GREEN}✓ Git installed${NC}"
else
  echo -e "${GREEN}✓ Git $(git --version | awk '{print $3}')${NC}"
fi

# ── Check & install Docker ────────────────────────────────────
if ! command -v docker &>/dev/null; then
  echo -e "${YELLOW}⚙  Docker not found. Installing...${NC}"
  if [[ "$OS" == "mac" ]]; then
    echo -e "${YELLOW}  Please install Docker Desktop for Mac: https://docs.docker.com/desktop/install/mac-install/${NC}"
    open "https://docs.docker.com/desktop/install/mac-install/" 2>/dev/null || true
    exit 1
  elif [[ "$OS" == "wsl" || "$OS" == "linux" ]]; then
    echo -e "${YELLOW}  Installing Docker Engine via official script...${NC}"
    curl -fsSL https://get.docker.com | sudo sh
    sudo usermod -aG docker "$USER"
    echo -e "${GREEN}✓ Docker installed.${NC}"
    echo -e "${YELLOW}  NOTE: Run: newgrp docker  (or log out/in) then re-run setup.sh${NC}"
    sudo systemctl start docker 2>/dev/null || true
  elif [[ "$OS" == "windows" ]]; then
    echo -e "${YELLOW}  Please install Docker Desktop: https://docs.docker.com/desktop/install/windows-install/${NC}"
    exit 1
  fi
else
  echo -e "${GREEN}✓ Docker $(docker --version | awk '{print $3}' | tr -d ',')${NC}"
fi

# ── Check Docker is running ───────────────────────────────────
if ! docker info &>/dev/null; then
  echo -e "${YELLOW}⚙  Docker daemon not running. Attempting to start...${NC}"
  if [[ "$OS" == "mac" || "$OS" == "windows" ]]; then
    open -a Docker 2>/dev/null || true
    echo -e "  Waiting for Docker Desktop to start (up to 30s)..."
    for i in $(seq 1 30); do
      sleep 1
      docker info &>/dev/null && break
      echo -n "."
    done
    echo ""
  else
    sudo systemctl start docker
    sleep 3
  fi
  if ! docker info &>/dev/null; then
    echo -e "${RED}✗ Docker is not running. Please start Docker and re-run this script.${NC}"
    exit 1
  fi
fi
echo -e "${GREEN}✓ Docker is running${NC}"

# ── Check & install Docker Compose ───────────────────────────
if ! docker compose version &>/dev/null; then
  if ! command -v docker-compose &>/dev/null; then
    echo -e "${YELLOW}⚙  Docker Compose not found. Installing plugin...${NC}"
    if [[ "$OS" == "linux" || "$OS" == "wsl" ]]; then
      COMPOSE_VERSION=$(curl -fsSL https://api.github.com/repos/docker/compose/releases/latest | grep '"tag_name"' | cut -d'"' -f4)
      DEST="/usr/local/lib/docker/cli-plugins"
      sudo mkdir -p "$DEST"
      sudo curl -fsSL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" \
        -o "${DEST}/docker-compose" || \
      sudo curl -fsSL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" \
        -o /usr/local/bin/docker-compose
      sudo chmod +x "${DEST}/docker-compose" 2>/dev/null || sudo chmod +x /usr/local/bin/docker-compose
    else
      install_pkg docker-compose-plugin 2>/dev/null || install_pkg docker-compose
    fi
    echo -e "${GREEN}✓ Docker Compose installed${NC}"
  fi
else
  echo -e "${GREEN}✓ Docker Compose $(docker compose version --short)${NC}"
fi

# ── .env setup ───────────────────────────────────────────────
if [ -f ".env" ]; then
  echo -e "\n${YELLOW}⚠  .env already exists. Overwrite? (y/N)${NC}"
  read -r overwrite
  if [[ ! "$overwrite" =~ ^[Yy]$ ]]; then
    echo "Keeping existing .env. Starting app..."
    launch_app
    exit 0
  fi
fi

echo -e "\n${BLUE}── Database ────────────────────────────${NC}"
read -rp "Postgres password [leave blank for auto-generated]: " POSTGRES_PASSWORD
if [ -z "$POSTGRES_PASSWORD" ]; then
  POSTGRES_PASSWORD=$(openssl rand -hex 16)
  echo -e "  ${GREEN}✓ Auto-generated: ${POSTGRES_PASSWORD}${NC}"
fi

echo -e "\n${BLUE}── JWT ─────────────────────────────────${NC}"
JWT_SECRET=$(openssl rand -base64 48 2>/dev/null || head -c 48 /dev/urandom | base64)
echo -e "  ${GREEN}✓ JWT secret auto-generated${NC}"

echo -e "\n${BLUE}── AI Provider ─────────────────────────${NC}"
echo "  1) Ollama — local & FREE (needs ~8GB RAM)"
echo "  2) OpenAI — GPT-4o-mini (needs API key)"
read -rp "Choose [1]: " ai_choice
ai_choice=${ai_choice:-1}

if [ "$ai_choice" = "2" ]; then
  AI_PROVIDER=openai
  read -rp "OpenAI API key (sk-...): " OPENAI_API_KEY
  OLLAMA_BASE_URL=""
  OLLAMA_MODEL=""
else
  AI_PROVIDER=ollama
  OPENAI_API_KEY=""
  OLLAMA_BASE_URL="http://ollama:11434"
  echo "  Models: llama3 (4.7GB, best) | phi3 (2.3GB, low RAM) | mistral (4.1GB)"
  read -rp "Ollama model [llama3]: " OLLAMA_MODEL
  OLLAMA_MODEL=${OLLAMA_MODEL:-llama3}
fi

echo -e "\n${BLUE}── Stripe (optional — press Enter to skip all) ──${NC}"
read -rp "Stripe secret key: " STRIPE_SECRET_KEY
read -rp "Stripe webhook secret: " STRIPE_WEBHOOK_SECRET
read -rp "Stripe price ID: " STRIPE_PRICE_ID

echo -e "\n${BLUE}── URLs ────────────────────────────────${NC}"
read -rp "Frontend URL [http://localhost:3000]: " FRONTEND_URL
FRONTEND_URL=${FRONTEND_URL:-http://localhost:3000}

cat > .env <<EOF
# Generated by setup.sh — $(date)
POSTGRES_USER=appuser
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}

JWT_SECRET=${JWT_SECRET}
JWT_ISSUER=CyprusInvoiceFixer
JWT_AUDIENCE=CyprusInvoiceFixerUsers

AI_PROVIDER=${AI_PROVIDER}
OPENAI_API_KEY=${OPENAI_API_KEY}
OLLAMA_BASE_URL=${OLLAMA_BASE_URL}
OLLAMA_MODEL=${OLLAMA_MODEL}

STRIPE_SECRET_KEY=${STRIPE_SECRET_KEY}
STRIPE_WEBHOOK_SECRET=${STRIPE_WEBHOOK_SECRET}
STRIPE_PRICE_ID=${STRIPE_PRICE_ID}

FRONTEND_URL=${FRONTEND_URL}
NEXT_PUBLIC_API_URL=http://localhost:5000
EOF

echo -e "\n${GREEN}✓ .env created${NC}"

# ── Launch ────────────────────────────────────────────────────
launch_app() {
  echo -e "\n${BLUE}⚙  Building and starting containers...${NC}"
  if [ "${AI_PROVIDER:-openai}" = "ollama" ]; then
    COMPOSE_PROFILES=ollama docker compose up --build -d
    echo -e "${YELLOW}⚡ Pulling Ollama model ${OLLAMA_MODEL:-llama3} (this may take a few minutes on first run)...${NC}"
    sleep 8
    docker compose exec ollama ollama pull "${OLLAMA_MODEL:-llama3}" || \
      echo -e "${YELLOW}  Model pull will retry on first use.${NC}"
  else
    docker compose up --build -d
  fi
}

launch_app

# ── Wait for API health ───────────────────────────────────────
echo -e "\n${BLUE}⏳ Waiting for API to be ready...${NC}"
for i in $(seq 1 30); do
  if curl -sf http://localhost:5000/health &>/dev/null; then
    break
  fi
  sleep 2
  echo -n "."
done
echo ""

echo -e "
${GREEN}╔══════════════════════════════════════════╗
║  ✅ Cyprus Invoice Fixer is running!     ║
╠══════════════════════════════════════════╣
║  🌐 App : ${FRONTEND_URL}
║  📖 API : http://localhost:5000/swagger
╚══════════════════════════════════════════╝${NC}
"

if command -v xdg-open &>/dev/null; then xdg-open "${FRONTEND_URL}" 2>/dev/null &
elif command -v open &>/dev/null; then open "${FRONTEND_URL}" 2>/dev/null &
fi
