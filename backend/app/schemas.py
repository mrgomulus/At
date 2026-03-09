from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional, List

class DisruptionBase(BaseModel):
    description: str = Field(..., min_length=10, max_length=2000)
    line_id: int
    sub_system_id: int
    category_id: Optional[int] = None
    duration_minutes: Optional[int] = None
    service_required: bool = False
    start_datetime: datetime
    end_datetime: Optional[datetime] = None
    notes: Optional[str] = None

class DisruptionCreate(DisruptionBase):
    pass

class DisruptionResponse(DisruptionBase):
    id: int
    disruption_number: str
    created_at: datetime
    updated_at: datetime
    archived: bool

    class Config:
        from_attributes = True

class LineBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=255)
    description: Optional[str] = None

class LineCreate(LineBase):
    pass

class LineResponse(LineBase):
    id: int
    created_at: datetime
    updated_at: datetime

class SubSystemBase(BaseModel):
    line_id: int
    name: str = Field(..., min_length=1, max_length=255)
    description: Optional[str] = None

class SubSystemCreate(SubSystemBase):
    pass

class SubSystemResponse(SubSystemBase):
    id: int
    created_at: datetime
    updated_at: datetime

class KPIResponse(BaseModel):
    total_disruptions: int
    disruptions_this_month: int
    total_duration_hours: float
    average_duration_hours: float
    with_service_required: int
    service_percentage: float
    trend: str
