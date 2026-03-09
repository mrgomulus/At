# Disruption Management System

## Overview

The Disruption Management System is designed to efficiently handle and mitigate disruptions in various operational processes. This project aims to provide tools and methodologies for proactively identifying, assessing, and responding to disruptions.

## Features

- **Real-time Monitoring**: Track disruptions using live data feeds.
- **Automated Response Protocols**: Pre-configured responses to common disruptions.
- **Dashboard**: A graphical interface for monitoring disruption metrics and analytics.
- **Reporting**: Generate reports on disruption frequency, impact, and resolutions.
- **REST API**: Full-featured backend API for managing disruptions, lines, and subsystems.
- **Modern Frontend**: React-based UI with TypeScript for type safety.

## Tech Stack

### Backend
- **Python 3.11+** with Flask
- **SQLAlchemy** for ORM
- **MySQL/SQLite** database support
- **Pydantic** for data validation
- **Flask-CORS** for cross-origin requests

### Frontend
- **React 18** with TypeScript
- **Vite** for fast development and building
- **Axios** for API communication
- **React Router** for navigation

### Infrastructure
- **Docker & Docker Compose** for containerization
- **Nginx** as reverse proxy
- **MySQL** for production database

## Installation

### Method 1: Using Docker (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/mrgomulus/At.git
   cd At
   ```

2. Start all services with Docker Compose:
   ```bash
   docker-compose up --build
   ```

3. Access the application:
   - Frontend: http://localhost (via Nginx)
   - Backend API: http://localhost/api
   - Direct Frontend: http://localhost:3000
   - Direct Backend: http://localhost:5000

### Method 2: Local Development

#### Backend Setup

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```

2. Create a virtual environment:
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

3. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

4. Start the backend server:
   ```bash
   python app.py
   ```

The backend will run on http://localhost:5000

#### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```

The frontend will run on http://localhost:3000

## Usage

### Dashboard
View real-time KPIs including:
- Total disruptions
- Disruptions this month
- Total and average duration
- Service requirement percentage
- Trend analysis

### Managing Disruptions
1. Navigate to the "Disruptions" page
2. Click "New Disruption" to create a disruption record
3. Fill in required information:
   - Description (10-2000 characters)
   - Production Line
   - SubSystem
   - Start date and time
   - Duration in minutes
   - Service requirement flag
   - Optional notes

### Settings
Configure production lines and subsystems:
1. Go to "Settings" page
2. Add production lines
3. Add subsystems for each line

## API Documentation

### Health Check
```bash
GET /api/health
```

### Lines
```bash
GET /api/lines              # List all lines
POST /api/lines             # Create a line
GET /api/lines/:id          # Get a specific line
```

### SubSystems
```bash
GET /api/subsystems         # List all subsystems
GET /api/subsystems?line_id=1  # Filter by line
POST /api/subsystems        # Create a subsystem
```

### Disruptions
```bash
GET /api/disruptions        # List disruptions (paginated)
POST /api/disruptions       # Create a disruption
GET /api/disruptions/:id    # Get a specific disruption
PUT /api/disruptions/:id    # Update a disruption
```

### KPIs
```bash
GET /api/kpis              # Get dashboard KPIs
```

## Configuration

### Backend Configuration
Edit `backend/app/config.py` for different environments:
- `DevelopmentConfig`: SQLite database
- `ProductionConfig`: MySQL database
- `TestingConfig`: Testing environment

### Environment Variables
- `DATABASE_URL`: Database connection string
- `FLASK_ENV`: Environment (development/production)
- `SECRET_KEY`: Secret key for session management

## Contributing

We welcome contributions! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Project Structure

```
At/
├── backend/
│   ├── app/
│   │   ├── __init__.py      # Flask app factory
│   │   ├── config.py        # Configuration classes
│   │   ├── database.py      # Database utilities
│   │   ├── models.py        # SQLAlchemy models
│   │   ├── routes.py        # API endpoints
│   │   └── schemas.py       # Pydantic schemas
│   ├── app.py               # Application entry point
│   ├── Dockerfile
│   └── requirements.txt
├── frontend/
│   ├── src/
│   │   ├── pages/           # React page components
│   │   ├── App.tsx          # Main app component
│   │   ├── api.ts           # API client
│   │   ├── types.ts         # TypeScript types
│   │   └── main.tsx         # Entry point
│   ├── Dockerfile
│   ├── package.json
│   └── vite.config.ts
├── docker-compose.yml
├── nginx.conf
└── README.md
```
