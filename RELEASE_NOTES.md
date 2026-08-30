# Cyprus Invoice Fixer v1.0.0

## 🎉 First Release

### What's included
- AI-powered invoice extraction (OpenAI GPT-4o-mini or local Ollama)
- Cyprus VAT compliance validation (all 14 mandatory fields)
- PDF export via QuestPDF
- JWT authentication + free/paid tiers
- Stripe Checkout integration
- Redis caching
- Full Next.js 14 frontend
- **First-run setup wizard** — input all secrets in-browser, no CLI needed

### How to install

#### Option A — Interactive setup (recommended)
```bash
tar -xzf cyprus-invoice-fixer-v1.0.0.tar.gz
bash setup.sh
```

#### Option B — Browser setup wizard
```bash
tar -xzf cyprus-invoice-fixer-v1.0.0.tar.gz
docker compose up -d
# Then open http://localhost:3000/setup
```

### Requirements
- Docker & Docker Compose v2+
- OpenAI API key **or** local GPU for Ollama
- (Optional) Stripe account for paid plans
