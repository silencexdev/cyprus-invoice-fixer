# 🧾 Cyprus Invoice Fixer

AI-powered Cyprus VAT invoice checker, fixer, and PDF exporter for freelancers and small businesses.

## Features
- Paste raw invoice text or upload an image
- AI-extracts supplier, customer, date, invoice #, VAT number, VAT rate, totals
- Missing-field checker against Cyprus VAT invoice mandatory requirements
- Clean invoice PDF export
- CSV export for accounting
- Free tier (3 invoices/month) + paid subscription

## Stack
- **Backend**: ASP.NET Core 8 Web API (C#)
- **Frontend**: React 18 + TypeScript + Vite
- **Database**: PostgreSQL + Entity Framework Core
- **AI**: OpenAI GPT-4o / local Ollama fallback
- **PDF**: QuestPDF
- **Auth**: JWT + refresh tokens
- **Cache**: Redis
- **Deployment**: Docker Compose

## Quick Start

```bash
cp .env.example .env
# Fill in your values in .env
docker compose up --build
```

- API: http://localhost:5000
- Frontend: http://localhost:3000
- Swagger: http://localhost:5000/swagger

## Development

### Backend
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```

## Environment Variables

See `.env.example` for all required variables.

## CI/CD

GitHub Actions runs on every push:
- Build & test backend
- Build & lint frontend
- Docker build check
- On tag `v*.*.*` → creates GitHub Release + Docker images

## License

MIT
