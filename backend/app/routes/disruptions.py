from fastapi import APIRouter, Depends, HTTPException, status, Query
from sqlalchemy.orm import Session
from typing import List, Optional
from datetime import datetime
from pydantic import BaseModel, Field
from ..database import get_db
from ..models import Disruption, Line, SubSystem, Category
import uuid

router = APIRouter()


def generate_disruption_number():
    now = datetime.utcnow()
    return f"DIS-{now.strftime('%Y%m')}-{str(uuid.uuid4())[:8].upper()}"


class DisruptionCreate(BaseModel):
    description: str = Field(..., min_length=10)
    line_id: Optional[int] = None
    sub_system_id: Optional[int] = None
    category_id: Optional[int] = None
    duration_minutes: Optional[int] = None
    service_required: bool = False
    start_datetime: datetime
    end_datetime: Optional[datetime] = None
    notes: Optional[str] = None
    severity: str = "medium"


class DisruptionUpdate(BaseModel):
    description: Optional[str] = None
    line_id: Optional[int] = None
    sub_system_id: Optional[int] = None
    category_id: Optional[int] = None
    duration_minutes: Optional[int] = None
    service_required: Optional[bool] = None
    start_datetime: Optional[datetime] = None
    end_datetime: Optional[datetime] = None
    notes: Optional[str] = None
    severity: Optional[str] = None
    archived: Optional[bool] = None


class DisruptionResponse(BaseModel):
    id: int
    disruption_number: str
    description: str
    line_id: Optional[int]
    sub_system_id: Optional[int]
    category_id: Optional[int]
    duration_minutes: Optional[int]
    service_required: bool
    start_datetime: datetime
    end_datetime: Optional[datetime]
    notes: Optional[str]
    archived: bool
    severity: str
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True


@router.get("/lines/all")
def list_lines(db: Session = Depends(get_db)):
    return db.query(Line).all()


@router.post("/lines/", status_code=201)
def create_line(name: str, description: Optional[str] = None, db: Session = Depends(get_db)):
    line = Line(name=name, description=description)
    db.add(line)
    db.commit()
    db.refresh(line)
    return line


@router.get("/categories/all")
def list_categories(db: Session = Depends(get_db)):
    return db.query(Category).all()


@router.post("/categories/", status_code=201)
def create_category(
    name: str,
    color: str = "#6366f1",
    description: Optional[str] = None,
    db: Session = Depends(get_db),
):
    cat = Category(name=name, color=color, description=description)
    db.add(cat)
    db.commit()
    db.refresh(cat)
    return cat


@router.get("/", response_model=List[DisruptionResponse])
def list_disruptions(
    archived: Optional[bool] = None,
    severity: Optional[str] = None,
    line_id: Optional[int] = None,
    skip: int = 0,
    limit: int = Query(50, le=200),
    db: Session = Depends(get_db),
):
    q = db.query(Disruption)
    if archived is not None:
        q = q.filter(Disruption.archived == archived)
    if severity:
        q = q.filter(Disruption.severity == severity)
    if line_id:
        q = q.filter(Disruption.line_id == line_id)
    return q.order_by(Disruption.start_datetime.desc()).offset(skip).limit(limit).all()


@router.post("/", response_model=DisruptionResponse, status_code=status.HTTP_201_CREATED)
def create_disruption(dis: DisruptionCreate, db: Session = Depends(get_db)):
    data = dis.model_dump()
    data["disruption_number"] = generate_disruption_number()
    db_dis = Disruption(**data)
    db.add(db_dis)
    db.commit()
    db.refresh(db_dis)
    return db_dis


@router.get("/{dis_id}", response_model=DisruptionResponse)
def get_disruption(dis_id: int, db: Session = Depends(get_db)):
    dis = db.query(Disruption).filter(Disruption.id == dis_id).first()
    if not dis:
        raise HTTPException(status_code=404, detail="Disruption not found")
    return dis


@router.put("/{dis_id}", response_model=DisruptionResponse)
def update_disruption(dis_id: int, update: DisruptionUpdate, db: Session = Depends(get_db)):
    dis = db.query(Disruption).filter(Disruption.id == dis_id).first()
    if not dis:
        raise HTTPException(status_code=404, detail="Disruption not found")
    for field, value in update.model_dump(exclude_unset=True).items():
        setattr(dis, field, value)
    db.commit()
    db.refresh(dis)
    return dis


@router.delete("/{dis_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_disruption(dis_id: int, db: Session = Depends(get_db)):
    dis = db.query(Disruption).filter(Disruption.id == dis_id).first()
    if not dis:
        raise HTTPException(status_code=404, detail="Disruption not found")
    db.delete(dis)
    db.commit()
