from fastapi import APIRouter, Depends, HTTPException, status, Query
from sqlalchemy.orm import Session
from typing import List, Optional
from datetime import datetime, timedelta
from pydantic import BaseModel
from ..database import get_db
from ..models import OpcUaVariable, VariableTracking, OpcUaConnection

router = APIRouter()


class VariableCreate(BaseModel):
    connection_id: int
    node_id: str
    variable_name: str
    display_name: Optional[str] = None
    data_type: Optional[str] = None
    unit: Optional[str] = None
    description: Optional[str] = None
    is_writable: bool = False
    sample_interval_ms: int = 1000


class VariableUpdate(BaseModel):
    variable_name: Optional[str] = None
    display_name: Optional[str] = None
    data_type: Optional[str] = None
    value: Optional[str] = None
    unit: Optional[str] = None
    description: Optional[str] = None
    is_writable: Optional[bool] = None
    sample_interval_ms: Optional[int] = None


class VariableResponse(BaseModel):
    id: int
    connection_id: int
    node_id: str
    variable_name: str
    display_name: Optional[str]
    data_type: Optional[str]
    value: Optional[str]
    unit: Optional[str]
    description: Optional[str]
    is_writable: bool
    sample_interval_ms: int
    last_updated: Optional[datetime]
    created_at: datetime

    class Config:
        from_attributes = True


class TrackingEntry(BaseModel):
    id: int
    variable_id: int
    timestamp: datetime
    value: Optional[str]
    quality: str

    class Config:
        from_attributes = True


@router.get("/", response_model=List[VariableResponse])
def list_variables(connection_id: Optional[int] = None, db: Session = Depends(get_db)):
    q = db.query(OpcUaVariable)
    if connection_id:
        q = q.filter(OpcUaVariable.connection_id == connection_id)
    return q.all()


@router.post("/", response_model=VariableResponse, status_code=status.HTTP_201_CREATED)
def create_variable(var: VariableCreate, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == var.connection_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    db_var = OpcUaVariable(**var.dict())
    db.add(db_var)
    db.commit()
    db.refresh(db_var)
    return db_var


@router.get("/{var_id}", response_model=VariableResponse)
def get_variable(var_id: int, db: Session = Depends(get_db)):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    return var


@router.put("/{var_id}", response_model=VariableResponse)
def update_variable(var_id: int, update: VariableUpdate, db: Session = Depends(get_db)):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    for field, value in update.dict(exclude_unset=True).items():
        setattr(var, field, value)
    db.commit()
    db.refresh(var)
    return var


@router.delete("/{var_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_variable(var_id: int, db: Session = Depends(get_db)):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    db.delete(var)
    db.commit()


@router.post("/{var_id}/record")
def record_value(var_id: int, value: str, quality: str = "Good", db: Session = Depends(get_db)):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    var.value = value
    var.last_updated = datetime.utcnow()
    tracking = VariableTracking(variable_id=var_id, value=value, quality=quality)
    db.add(tracking)
    db.commit()
    return {"status": "recorded", "variable_id": var_id, "value": value}


@router.get("/{var_id}/history", response_model=List[TrackingEntry])
def variable_history(
    var_id: int,
    hours: int = Query(24, description="Hours of history to return"),
    db: Session = Depends(get_db),
):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    since = datetime.utcnow() - timedelta(hours=hours)
    return (
        db.query(VariableTracking)
        .filter(VariableTracking.variable_id == var_id, VariableTracking.timestamp >= since)
        .order_by(VariableTracking.timestamp.desc())
        .all()
    )


@router.get("/{var_id}/stats")
def variable_stats(var_id: int, hours: int = 24, db: Session = Depends(get_db)):
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == var_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    since = datetime.utcnow() - timedelta(hours=hours)
    records = (
        db.query(VariableTracking)
        .filter(VariableTracking.variable_id == var_id, VariableTracking.timestamp >= since)
        .all()
    )
    values = []
    for r in records:
        try:
            values.append(float(r.value))
        except (TypeError, ValueError):
            pass
    stats = {
        "variable_id": var_id,
        "variable_name": var.variable_name,
        "current_value": var.value,
        "last_updated": var.last_updated,
        "total_records": len(records),
        "numeric_records": len(values),
    }
    if values:
        stats["min_value"] = min(values)
        stats["max_value"] = max(values)
        stats["avg_value"] = sum(values) / len(values)
    return stats
