# Quick Start Guide

This guide will help you get the Disruption Management System up and running in minutes.

## Prerequisites

- Docker and Docker Compose installed
- OR Python 3.11+ and Node.js 18+ for local development

## Option 1: Docker Quick Start (Recommended)

### 1. Clone and Start

```bash
git clone https://github.com/mrgomulus/At.git
cd At
docker-compose up --build
```

### 2. Wait for Services

Wait for all services to start (this may take a few minutes on first run):
- ✅ MySQL database
- ✅ Backend Flask API
- ✅ Frontend React app
- ✅ Nginx reverse proxy

### 3. Access the Application

Open your browser and navigate to:
- **Main Application**: http://localhost
- **API Documentation**: http://localhost/api/health

### 4. Initialize Sample Data (Optional)

Open a new terminal and run:

```bash
docker-compose exec backend python init_sample_data.py
```

This will create sample production lines, subsystems, and disruptions to explore the system.

## Option 2: Local Development Quick Start

### Backend

```bash
cd backend
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt
python app.py
```

Backend runs on http://localhost:5000

### Frontend

Open a new terminal:

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on http://localhost:3000

### Initialize Sample Data

```bash
cd backend
python init_sample_data.py
```

## First Steps

1. **Explore the Dashboard**: View KPIs and recent disruptions
2. **Configure Settings**: 
   - Add production lines (e.g., "Assembly Line A")
   - Add subsystems for each line (e.g., "Conveyor Belt")
3. **Create a Disruption**:
   - Go to "Disruptions" page
   - Click "New Disruption"
   - Fill in the form and submit

## API Examples

### Health Check
```bash
curl http://localhost:5000/api/health
```

### Create a Line
```bash
curl -X POST http://localhost:5000/api/lines \
  -H "Content-Type: application/json" \
  -d '{"name": "Production Line 1", "description": "Main line"}'
```

### Get All Disruptions
```bash
curl http://localhost:5000/api/disruptions
```

### Get KPIs
```bash
curl http://localhost:5000/api/kpis
```

## Troubleshooting

### Docker Issues

**Services won't start:**
```bash
docker-compose down
docker-compose up --build
```

**Database connection errors:**
Wait a bit longer for MySQL to fully initialize (can take 30-60 seconds on first run).

**Port conflicts:**
Make sure ports 80, 3000, 5000, and 3306 are not in use by other applications.

### Local Development Issues

**Backend: Database errors:**
The backend uses SQLite by default in development mode. The database file will be created automatically.

**Frontend: npm install fails:**
Make sure you have Node.js 18 or higher installed:
```bash
node --version
```

**CORS errors:**
Make sure the backend is running before starting the frontend.

## Next Steps

- Read the [README.md](README.md) for detailed documentation
- Check the [PROJEKT_DOKUMENTATION.md](PROJEKT_DOKUMENTATION.md) for technical details (German)
- Explore the API endpoints in `backend/app/routes.py`
- Customize the frontend in `frontend/src/`

## Support

For issues or questions:
1. Check existing GitHub issues
2. Create a new issue with details about your problem
3. Include error messages and steps to reproduce

Happy disruption managing! 🚀
