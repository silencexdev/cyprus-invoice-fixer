# Cyprus Invoice Fixer

> AI-powered Cyprus VAT invoice checker, fixer, and PDF exporter.

## Features

- 🤖 **AI extraction** — paste any invoice text or upload an image; GPT-4o-mini (or local Ollama) extracts every field automatically
- ✅ **Cyprus VAT validation** — checks all 14 mandatory fields including VAT number format, standard rates (5% / 9% / 19%), and totals
- 📄 **PDF export** — generates a clean, compliant A4 invoice PDF via QuestPDF
- 🔐 **JWT auth** — register / login, all invoice data is user-scoped
- 💳 **Stripe billing** — free tier (3 invoices/month), paid plan via Stripe Checkout
- ⚡ **Redis caching** — session-level caching layer
- 🐳 **Docker Compose** — one command to run everything

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend API | ASP.NET Core 8, C# 12 |
| Database | PostgreSQL 16 + EF Core 8 |
| Cache | Redis 7 |
| AI | OpenAI GPT-4o-mini (or Ollama) |
| PDF | QuestPDF |
| Auth | JWT Bearer |
| Payments | Stripe Checkout |
| Frontend | Next.js 14, TypeScript, Tailwind CSS |
| Containerisation | Docker + Docker Compose |

## Quick Start

### Prerequisites
- Docker & Docker Compose
- An OpenAI API key (or local Ollama)
- A Stripe account (for billing features)

### 1. Clone & configure

```bash
git clone https://github.com/silencexdev/cyprus-invoice-fixer.git
cd cyprus-invoice-fixer
cp .env.example .env
# Edit .env with your secrets
```

### 2. Run

```bash
docker compose up --build
```

- Frontend: http://localhost:3000
- API + Swagger: http://localhost:5000/swagger

### 3. Optional: Use Ollama instead of OpenAI

```bash
# In .env:
AI_PROVIDER=ollama

# Start with Ollama profile:
docker compose --profile ollama up --build

# Pull a model:
docker exec -it cyprus-invoice-fixer-ollama-1 ollama pull llama3
```

## API Reference

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | — | Register new user |
| POST | `/api/auth/login` | — | Login, get JWT |
| GET | `/api/me` | ✓ | Current user + usage |
| POST | `/api/invoice/parse/text` | ✓ | Parse invoice from text |
| POST | `/api/invoice/parse/image` | ✓ | Parse invoice from image |
| GET | `/api/invoice` | ✓ | List invoices (paginated) |
| GET | `/api/invoice/{id}` | ✓ | Get invoice |
| DELETE | `/api/invoice/{id}` | ✓ | Delete invoice |
| GET | `/api/invoice/{id}/pdf` | ✓ | Download PDF |
| GET | `/api/invoice/{id}/validate` | ✓ | Re-validate invoice |
| POST | `/api/billing/checkout` | ✓ | Create Stripe checkout |
| POST | `/api/billing/webhook` | — | Stripe webhook |

## Project Structure

```
cyprus-invoice-fixer/
├── backend/
│   ├── CyprusInvoiceFixer.Core/     # Models, DbContext, all services
│   └── CyprusInvoiceFixer.Api/      # Controllers, validators, Program.cs
├── frontend/                        # Next.js 14 app
├── docker-compose.yml
├── .env.example
└── README.md
```

## Environment Variables

See [`.env.example`](.env.example) for all required variables.

## License

MIT
