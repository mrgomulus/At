from sqlalchemy import Column, Integer, String, Float, Boolean, DateTime, Text, ForeignKey, JSON
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from .database import Base


class OpcUaConnection(Base):
    __tablename__ = "opc_ua_connections"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False)
    url = Column(String(500), nullable=False)
    namespace = Column(String(255), nullable=True)
    username = Column(String(255), nullable=True)
    password = Column(String(255), nullable=True)
    security_mode = Column(String(50), default="None")
    security_policy = Column(String(100), default="Basic256Sha256")
    description = Column(Text, nullable=True)
    is_active = Column(Boolean, default=True)
    status = Column(String(50), default="disconnected")
    last_connected = Column(DateTime, nullable=True)
    created_at = Column(DateTime, server_default=func.now())
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    variables = relationship("OpcUaVariable", back_populates="connection", cascade="all, delete-orphan")
    favorites = relationship("ConnectionFavorite", back_populates="connection", cascade="all, delete-orphan")
    diagnostic_sessions = relationship("DiagnosticSession", back_populates="connection", cascade="all, delete-orphan")


class OpcUaVariable(Base):
    __tablename__ = "opc_ua_variables"
    id = Column(Integer, primary_key=True, index=True)
    connection_id = Column(Integer, ForeignKey("opc_ua_connections.id"), nullable=False)
    node_id = Column(String(255), nullable=False)
    variable_name = Column(String(255), nullable=False)
    display_name = Column(String(255), nullable=True)
    data_type = Column(String(50), nullable=True)
    value = Column(Text, nullable=True)
    unit = Column(String(50), nullable=True)
    description = Column(Text, nullable=True)
    is_writable = Column(Boolean, default=False)
    sample_interval_ms = Column(Integer, default=1000)
    last_updated = Column(DateTime, nullable=True)
    created_at = Column(DateTime, server_default=func.now())

    connection = relationship("OpcUaConnection", back_populates="variables")
    tracking = relationship("VariableTracking", back_populates="variable", cascade="all, delete-orphan")
    favorites = relationship("ConnectionFavorite", back_populates="variable")


class ConnectionFavorite(Base):
    __tablename__ = "connection_favorites"
    id = Column(Integer, primary_key=True, index=True)
    connection_id = Column(Integer, ForeignKey("opc_ua_connections.id"), nullable=False)
    variable_id = Column(Integer, ForeignKey("opc_ua_variables.id"), nullable=False)
    custom_label = Column(String(255), nullable=True)
    custom_color = Column(String(7), nullable=True)
    show_in_dashboard = Column(Boolean, default=True)
    alarm_enabled = Column(Boolean, default=False)
    alarm_threshold_min = Column(Float, nullable=True)
    alarm_threshold_max = Column(Float, nullable=True)
    sort_order = Column(Integer, default=0)
    notes = Column(Text, nullable=True)
    created_at = Column(DateTime, server_default=func.now())
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    connection = relationship("OpcUaConnection", back_populates="favorites")
    variable = relationship("OpcUaVariable", back_populates="favorites")


class VariableTracking(Base):
    __tablename__ = "variable_tracking"
    id = Column(Integer, primary_key=True, index=True)
    variable_id = Column(Integer, ForeignKey("opc_ua_variables.id"), nullable=False)
    timestamp = Column(DateTime, server_default=func.now())
    value = Column(Text, nullable=True)
    quality = Column(String(50), default="Good")
    source_timestamp = Column(DateTime, nullable=True)

    variable = relationship("OpcUaVariable", back_populates="tracking")


class DiagnosticSession(Base):
    __tablename__ = "diagnostic_sessions"
    id = Column(Integer, primary_key=True, index=True)
    connection_id = Column(Integer, ForeignKey("opc_ua_connections.id"), nullable=False)
    session_type = Column(String(100), nullable=False)
    status = Column(String(50), default="running")
    result = Column(Text, nullable=True)
    details = Column(JSON, nullable=True)
    started_at = Column(DateTime, server_default=func.now())
    completed_at = Column(DateTime, nullable=True)
    duration_ms = Column(Integer, nullable=True)
    error_message = Column(Text, nullable=True)

    connection = relationship("OpcUaConnection", back_populates="diagnostic_sessions")


class Line(Base):
    __tablename__ = "lines"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False, unique=True)
    description = Column(Text, nullable=True)
    created_at = Column(DateTime, server_default=func.now())
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    sub_systems = relationship("SubSystem", back_populates="line", cascade="all, delete-orphan")
    disruptions = relationship("Disruption", back_populates="line")


class SubSystem(Base):
    __tablename__ = "sub_systems"
    id = Column(Integer, primary_key=True, index=True)
    line_id = Column(Integer, ForeignKey("lines.id"), nullable=False)
    name = Column(String(255), nullable=False)
    description = Column(Text, nullable=True)
    created_at = Column(DateTime, server_default=func.now())
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    line = relationship("Line", back_populates="sub_systems")
    disruptions = relationship("Disruption", back_populates="sub_system")


class Category(Base):
    __tablename__ = "categories"
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False, unique=True)
    color = Column(String(7), default="#6366f1")
    description = Column(Text, nullable=True)
    created_at = Column(DateTime, server_default=func.now())

    disruptions = relationship("Disruption", back_populates="category")


class Disruption(Base):
    __tablename__ = "disruptions"
    id = Column(Integer, primary_key=True, index=True)
    disruption_number = Column(String(50), unique=True, nullable=False)
    description = Column(Text, nullable=False)
    line_id = Column(Integer, ForeignKey("lines.id"), nullable=True)
    sub_system_id = Column(Integer, ForeignKey("sub_systems.id"), nullable=True)
    category_id = Column(Integer, ForeignKey("categories.id"), nullable=True)
    duration_minutes = Column(Integer, nullable=True)
    service_required = Column(Boolean, default=False)
    start_datetime = Column(DateTime, nullable=False)
    end_datetime = Column(DateTime, nullable=True)
    notes = Column(Text, nullable=True)
    archived = Column(Boolean, default=False)
    severity = Column(String(20), default="medium")
    created_at = Column(DateTime, server_default=func.now())
    updated_at = Column(DateTime, server_default=func.now(), onupdate=func.now())

    line = relationship("Line", back_populates="disruptions")
    sub_system = relationship("SubSystem", back_populates="disruptions")
    category = relationship("Category", back_populates="disruptions")
