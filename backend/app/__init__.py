from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from .database import engine, Base
from .routes import connections, variables, favorites, diagnostics, disruptions, kpis


def create_app() -> FastAPI:
    app = FastAPI(title="Disruption Management System", version="2.0.0")

    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    app.include_router(connections.router, prefix="/api/connections", tags=["connections"])
    app.include_router(variables.router, prefix="/api/variables", tags=["variables"])
    app.include_router(favorites.router, prefix="/api/favorites", tags=["favorites"])
    app.include_router(diagnostics.router, prefix="/api/diagnostics", tags=["diagnostics"])
    app.include_router(disruptions.router, prefix="/api/disruptions", tags=["disruptions"])
    app.include_router(kpis.router, prefix="/api/kpis", tags=["kpis"])

    return app
