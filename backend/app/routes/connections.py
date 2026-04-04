from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List, Optional
from datetime import datetime
from pydantic import BaseModel
from ..database import get_db
from ..models import OpcUaConnection

router = APIRouter()


class ConnectionCreate(BaseModel):
    name: str
    url: str
    namespace: Optional[str] = None
    username: Optional[str] = None
    password: Optional[str] = None
    security_mode: str = "None"
    security_policy: str = "Basic256Sha256"
    description: Optional[str] = None


class ConnectionUpdate(BaseModel):
    name: Optional[str] = None
    url: Optional[str] = None
    namespace: Optional[str] = None
    username: Optional[str] = None
    password: Optional[str] = None
    security_mode: Optional[str] = None
    security_policy: Optional[str] = None
    description: Optional[str] = None
    is_active: Optional[bool] = None


class ConnectionResponse(BaseModel):
    id: int
    name: str
    url: str
    namespace: Optional[str]
    username: Optional[str]
    security_mode: str
    security_policy: str
    description: Optional[str]
    is_active: bool
    status: str
    last_connected: Optional[datetime]
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True


@router.get("/", response_model=List[ConnectionResponse])
def list_connections(db: Session = Depends(get_db)):
    return db.query(OpcUaConnection).all()


@router.post("/", response_model=ConnectionResponse, status_code=status.HTTP_201_CREATED)
def create_connection(conn: ConnectionCreate, db: Session = Depends(get_db)):
    db_conn = OpcUaConnection(**conn.dict())
    db.add(db_conn)
    db.commit()
    db.refresh(db_conn)
    return db_conn


@router.get("/{conn_id}", response_model=ConnectionResponse)
def get_connection(conn_id: int, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    return conn


@router.put("/{conn_id}", response_model=ConnectionResponse)
def update_connection(conn_id: int, update: ConnectionUpdate, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    for field, value in update.dict(exclude_unset=True).items():
        setattr(conn, field, value)
    db.commit()
    db.refresh(conn)
    return conn


@router.delete("/{conn_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_connection(conn_id: int, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    db.delete(conn)
    db.commit()


@router.post("/{conn_id}/test")
def test_connection(conn_id: int, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    try:
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
        result = sock.connect_ex((host, port))
        sock.close()
        if result == 0:
            conn.status = "connected"
            conn.last_connected = datetime.utcnow()
            db.commit()
            return {"status": "connected", "message": f"Successfully connected to {conn.url}"}
        else:
            conn.status = "error"
            db.commit()
            return {"status": "error", "message": f"Cannot reach {conn.url}"}
    except Exception as e:
        conn.status = "error"
        db.commit()
        return {"status": "error", "message": str(e)}


@router.get("/{conn_id}/stats")
def connection_stats(conn_id: int, db: Session = Depends(get_db)):
    from ..models import OpcUaVariable, ConnectionFavorite, DiagnosticSession
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == conn_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    variable_count = db.query(OpcUaVariable).filter(OpcUaVariable.connection_id == conn_id).count()
    favorite_count = db.query(ConnectionFavorite).filter(ConnectionFavorite.connection_id == conn_id).count()
    session_count = db.query(DiagnosticSession).filter(DiagnosticSession.connection_id == conn_id).count()
    return {
        "connection_id": conn_id,
        "name": conn.name,
        "status": conn.status,
        "variable_count": variable_count,
        "favorite_count": favorite_count,
        "diagnostic_session_count": session_count,
        "last_connected": conn.last_connected,
    }
