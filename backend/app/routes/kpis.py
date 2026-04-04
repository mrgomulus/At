from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from sqlalchemy import func, text
from datetime import datetime, timedelta
from ..database import get_db
from ..models import (
    Disruption,
    OpcUaConnection,
    OpcUaVariable,
    VariableTracking,
    DiagnosticSession,
    ConnectionFavorite,
)

router = APIRouter()


@router.get("/")
def get_kpis(db: Session = Depends(get_db)):
    now = datetime.utcnow()
    month_start = now.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    last_month_start = (month_start - timedelta(days=1)).replace(day=1)

    total_disruptions = db.query(Disruption).filter(Disruption.archived == False).count()  # noqa: E712
    this_month = db.query(Disruption).filter(Disruption.start_datetime >= month_start).count()
    last_month = db.query(Disruption).filter(
        Disruption.start_datetime >= last_month_start,
        Disruption.start_datetime < month_start,
    ).count()

    duration_result = (
        db.query(func.sum(Disruption.duration_minutes)).filter(Disruption.archived == False).scalar() or 0  # noqa: E712
    )
    avg_duration = (
        db.query(func.avg(Disruption.duration_minutes)).filter(Disruption.archived == False).scalar() or 0  # noqa: E712
    )
    service_required = db.query(Disruption).filter(
        Disruption.service_required == True, Disruption.archived == False  # noqa: E712
    ).count()

    critical = db.query(Disruption).filter(
        Disruption.severity == "critical", Disruption.archived == False  # noqa: E712
    ).count()
    high = db.query(Disruption).filter(
        Disruption.severity == "high", Disruption.archived == False  # noqa: E712
    ).count()

    total_connections = db.query(OpcUaConnection).count()
    active_connections = db.query(OpcUaConnection).filter(OpcUaConnection.status == "connected").count()
    total_variables = db.query(OpcUaVariable).count()
    total_favorites = db.query(ConnectionFavorite).count()
    alarms_active = db.query(ConnectionFavorite).filter(ConnectionFavorite.alarm_enabled == True).count()  # noqa: E712

    recent_tracking = db.query(VariableTracking).filter(
        VariableTracking.timestamp >= now - timedelta(hours=1)
    ).count()

    recent_diagnostics = db.query(DiagnosticSession).filter(
        DiagnosticSession.started_at >= now - timedelta(hours=24)
    ).count()

    trend = "stable"
    if this_month > last_month * 1.2:
        trend = "increasing"
    elif this_month < last_month * 0.8:
        trend = "decreasing"

    return {
        "disruptions": {
            "total": total_disruptions,
            "this_month": this_month,
            "last_month": last_month,
            "total_duration_hours": round(duration_result / 60, 2),
            "avg_duration_hours": round(float(avg_duration) / 60, 2) if avg_duration else 0,
            "service_required": service_required,
            "service_percentage": round(service_required / total_disruptions * 100, 1) if total_disruptions > 0 else 0,
            "critical": critical,
            "high": high,
            "trend": trend,
        },
        "connections": {
            "total": total_connections,
            "active": active_connections,
            "total_variables": total_variables,
            "total_favorites": total_favorites,
            "alarms_active": alarms_active,
        },
        "monitoring": {
            "tracking_records_last_hour": recent_tracking,
            "diagnostic_sessions_last_24h": recent_diagnostics,
        },
    }


@router.get("/health")
def health_check(db: Session = Depends(get_db)):
    try:
        db.execute(text("SELECT 1"))
        db_status = "healthy"
    except Exception:
        db_status = "error"
    return {
        "status": "ok",
        "timestamp": datetime.utcnow().isoformat(),
        "database": db_status,
    }
