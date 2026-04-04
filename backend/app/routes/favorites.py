from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session, joinedload
from typing import List, Optional
from datetime import datetime
from pydantic import BaseModel
from ..database import get_db
from ..models import ConnectionFavorite, OpcUaVariable, OpcUaConnection

router = APIRouter()


class FavoriteCreate(BaseModel):
    connection_id: int
    variable_id: int
    custom_label: Optional[str] = None
    custom_color: Optional[str] = None
    show_in_dashboard: bool = True
    alarm_enabled: bool = False
    alarm_threshold_min: Optional[float] = None
    alarm_threshold_max: Optional[float] = None
    sort_order: int = 0
    notes: Optional[str] = None


class FavoriteUpdate(BaseModel):
    custom_label: Optional[str] = None
    custom_color: Optional[str] = None
    show_in_dashboard: Optional[bool] = None
    alarm_enabled: Optional[bool] = None
    alarm_threshold_min: Optional[float] = None
    alarm_threshold_max: Optional[float] = None
    sort_order: Optional[int] = None
    notes: Optional[str] = None


class VariableInfo(BaseModel):
    id: int
    node_id: str
    variable_name: str
    display_name: Optional[str]
    data_type: Optional[str]
    value: Optional[str]
    unit: Optional[str]
    last_updated: Optional[datetime]

    class Config:
        from_attributes = True


class FavoriteResponse(BaseModel):
    id: int
    connection_id: int
    variable_id: int
    custom_label: Optional[str]
    custom_color: Optional[str]
    show_in_dashboard: bool
    alarm_enabled: bool
    alarm_threshold_min: Optional[float]
    alarm_threshold_max: Optional[float]
    sort_order: int
    notes: Optional[str]
    created_at: datetime
    updated_at: datetime
    variable: Optional[VariableInfo]

    class Config:
        from_attributes = True


@router.get("/", response_model=List[FavoriteResponse])
def list_favorites(connection_id: Optional[int] = None, db: Session = Depends(get_db)):
    q = db.query(ConnectionFavorite).options(joinedload(ConnectionFavorite.variable))
    if connection_id:
        q = q.filter(ConnectionFavorite.connection_id == connection_id)
    return q.order_by(ConnectionFavorite.sort_order).all()


@router.post("/", response_model=FavoriteResponse, status_code=status.HTTP_201_CREATED)
def add_favorite(fav: FavoriteCreate, db: Session = Depends(get_db)):
    conn = db.query(OpcUaConnection).filter(OpcUaConnection.id == fav.connection_id).first()
    if not conn:
        raise HTTPException(status_code=404, detail="Connection not found")
    var = db.query(OpcUaVariable).filter(OpcUaVariable.id == fav.variable_id).first()
    if not var:
        raise HTTPException(status_code=404, detail="Variable not found")
    existing = db.query(ConnectionFavorite).filter(
        ConnectionFavorite.connection_id == fav.connection_id,
        ConnectionFavorite.variable_id == fav.variable_id,
    ).first()
    if existing:
        raise HTTPException(status_code=400, detail="Variable already in favorites for this connection")
    db_fav = ConnectionFavorite(**fav.model_dump())
    db.add(db_fav)
    db.commit()
    db.refresh(db_fav)
    db_fav = (
        db.query(ConnectionFavorite)
        .options(joinedload(ConnectionFavorite.variable))
        .filter(ConnectionFavorite.id == db_fav.id)
        .first()
    )
    return db_fav


@router.get("/dashboard/all")
def dashboard_favorites(db: Session = Depends(get_db)):
    """Get all favorites marked for dashboard display, with current alarm status."""
    favs = (
        db.query(ConnectionFavorite)
        .options(joinedload(ConnectionFavorite.variable), joinedload(ConnectionFavorite.connection))
        .filter(ConnectionFavorite.show_in_dashboard == True)  # noqa: E712
        .order_by(ConnectionFavorite.sort_order)
        .all()
    )
    result = []
    for fav in favs:
        alarm_status = None
        if fav.alarm_enabled and fav.variable and fav.variable.value:
            try:
                val = float(fav.variable.value)
                if fav.alarm_threshold_min is not None and val < fav.alarm_threshold_min:
                    alarm_status = "below_min"
                elif fav.alarm_threshold_max is not None and val > fav.alarm_threshold_max:
                    alarm_status = "above_max"
                else:
                    alarm_status = "normal"
            except (TypeError, ValueError):
                alarm_status = "non_numeric"
        result.append({
            "favorite_id": fav.id,
            "connection_id": fav.connection_id,
            "connection_name": fav.connection.name if fav.connection else None,
            "variable_id": fav.variable_id,
            "variable_name": fav.variable.variable_name if fav.variable else None,
            "display_name": fav.custom_label or (fav.variable.display_name if fav.variable else None),
            "value": fav.variable.value if fav.variable else None,
            "unit": fav.variable.unit if fav.variable else None,
            "data_type": fav.variable.data_type if fav.variable else None,
            "last_updated": fav.variable.last_updated if fav.variable else None,
            "custom_color": fav.custom_color,
            "alarm_enabled": fav.alarm_enabled,
            "alarm_status": alarm_status,
            "alarm_threshold_min": fav.alarm_threshold_min,
            "alarm_threshold_max": fav.alarm_threshold_max,
            "notes": fav.notes,
            "sort_order": fav.sort_order,
        })
    return result


@router.get("/{fav_id}", response_model=FavoriteResponse)
def get_favorite(fav_id: int, db: Session = Depends(get_db)):
    fav = (
        db.query(ConnectionFavorite)
        .options(joinedload(ConnectionFavorite.variable))
        .filter(ConnectionFavorite.id == fav_id)
        .first()
    )
    if not fav:
        raise HTTPException(status_code=404, detail="Favorite not found")
    return fav


@router.put("/{fav_id}", response_model=FavoriteResponse)
def update_favorite(fav_id: int, update: FavoriteUpdate, db: Session = Depends(get_db)):
    fav = db.query(ConnectionFavorite).filter(ConnectionFavorite.id == fav_id).first()
    if not fav:
        raise HTTPException(status_code=404, detail="Favorite not found")
    for field, value in update.model_dump(exclude_unset=True).items():
        setattr(fav, field, value)
    db.commit()
    db.refresh(fav)
    fav = (
        db.query(ConnectionFavorite)
        .options(joinedload(ConnectionFavorite.variable))
        .filter(ConnectionFavorite.id == fav_id)
        .first()
    )
    return fav


@router.delete("/{fav_id}", status_code=status.HTTP_204_NO_CONTENT)
def remove_favorite(fav_id: int, db: Session = Depends(get_db)):
    fav = db.query(ConnectionFavorite).filter(ConnectionFavorite.id == fav_id).first()
    if not fav:
        raise HTTPException(status_code=404, detail="Favorite not found")
    db.delete(fav)
    db.commit()
