from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List, Optional
from datetime import datetime, timedelta
from pydantic import BaseModel
from ..database import get_db
from ..models import DiagnosticSession, OpcUaConnection, OpcUaVariable, VariableTracking

router = APIRouter()


class DiagnosticSessionResponse(BaseModel):
    id: int
    connection_id: int
    session_type: str
    status: str
    result: Optional[str]
    details: Optional[dict]
    started_at: datetime
    completed_at: Optional[datetime]
    duration_ms: Optional[int]
    error_message: Optional[str]

    class Config:
        from_attributes = True


@router.get("/sessions", response_model=List[DiagnosticSessionResponse])
def list_sessions(connection_id: Optional[int] = None, db: Session = Depends(get_db)):
    q = db.query(DiagnosticSession)
    if connection_id:
        q = q.filter(DiagnosticSession.connection_id == connection_id)
    return q.order_by(DiagnosticSession.started_at.desc()).limit(100).all()


@router.post("/run/{conn_id}")
def run_diagnostic(conn_id: int, session_type: str = "health_check", db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")

    start_time = datetime.utcnow()
    session = DiagnosticSession(connection_id=conn_id, session_type=session_type, status="running")
    db.add(session)
    db.commit()
    db.refresh(session)

    details = {}
    result = "passed"
    error_message = None

    try:
        if session_type == "connection_test":
            import socket
            url = conn.url.replace("opc.tcp://", "")
            if ":" in url:
                host, port_str = url.split(":", 1)
                port = int(port_str.split("/")[0])
            else:
                host = url
                port = 4840
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(3)
            rc = sock.connect_ex((host, port))
            sock.close()
            details["host"] = host
            details["port"] = port
            details["reachable"] = rc == 0
            if rc != 0:
                result = "failed"
                error_message = f"Port {port} not reachable on {host}"

        elif session_type == "health_check":
            var_count = db.query(OpcUaVariable).filter(OpcUaVariable.connection_id == conn_id).count()
            recent_cutoff = datetime.utcnow() - timedelta(hours=1)
            recent_updates = db.query(OpcUaVariable).filter(
                OpcUaVariable.connection_id == conn_id,
                OpcUaVariable.last_updated >= recent_cutoff,
            ).count()
            details["total_variables"] = var_count
            details["recently_updated_variables"] = recent_updates
            details["connection_status"] = conn.status
            details["last_connected"] = conn.last_connected.isoformat() if conn.last_connected else None
            if conn.status == "error":
                result = "warning"

        elif session_type == "full_scan":
            var_count = db.query(OpcUaVariable).filter(OpcUaVariable.connection_id == conn_id).count()
            tracking_count = (
                db.query(VariableTracking)
                .join(OpcUaVariable)
                .filter(OpcUaVariable.connection_id == conn_id)
                .count()
            )
            details["total_variables"] = var_count
            details["total_tracking_records"] = tracking_count
            details["connection_name"] = conn.name
            details["connection_url"] = conn.url
            details["is_active"] = conn.is_active

        session.status = "completed"
        session.result = result
        session.details = details
        session.completed_at = datetime.utcnow()
        session.duration_ms = int((datetime.utcnow() - start_time).total_seconds() * 1000)
        session.error_message = error_message
    except Exception as e:
        session.status = "failed"
        session.result = "error"
        session.error_message = str(e)
        session.completed_at = datetime.utcnow()
        session.duration_ms = int((datetime.utcnow() - start_time).total_seconds() * 1000)

    db.commit()
    db.refresh(session)
    return session


@router.get("/sessions/{session_id}", response_model=DiagnosticSessionResponse)
def get_session(session_id: int, db: Session = Depends(get_db)):
    session = db.query(DiagnosticSession).filter(DiagnosticSession.id == session_id).first()
    if not session:
        raise HTTPException(status_code=404, detail="Diagnostic session not found")
    return session


@router.delete("/sessions/{session_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_session(session_id: int, db: Session = Depends(get_db)):
    session = db.query(DiagnosticSession).filter(DiagnosticSession.id == session_id).first()
    if not session:
        raise HTTPException(status_code=404, detail="Diagnostic session not found")
    db.delete(session)
    db.commit()


@router.get("/summary")
def diagnostics_summary(db: Session = Depends(get_db)):
    total_sessions = db.query(DiagnosticSession).count()
    passed = db.query(DiagnosticSession).filter(DiagnosticSession.result == "passed").count()
    failed = db.query(DiagnosticSession).filter(DiagnosticSession.result == "failed").count()
    warnings = db.query(DiagnosticSession).filter(DiagnosticSession.result == "warning").count()
    total_connections = db.query(OpcUaConnection).count()
    connected = db.query(OpcUaConnection).filter(OpcUaConnection.status == "connected").count()
    return {
        "total_sessions": total_sessions,
        "passed": passed,
        "failed": failed,
        "warnings": warnings,
        "total_connections": total_connections,
        "connected_connections": connected,
        "pass_rate": round(passed / total_sessions * 100, 1) if total_sessions > 0 else 0,
    }
