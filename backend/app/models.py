from datetime import datetime
from app import db

class Line(db.Model):
    __tablename__ = 'lines'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(255), nullable=False)
    description = db.Column(db.Text)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    subsystems = db.relationship('SubSystem', backref='line', lazy=True, cascade='all, delete-orphan')
    disruptions = db.relationship('Disruption', backref='line', lazy=True)

class SubSystem(db.Model):
    __tablename__ = 'subsystems'
    
    id = db.Column(db.Integer, primary_key=True)
    line_id = db.Column(db.Integer, db.ForeignKey('lines.id'), nullable=False)
    name = db.Column(db.String(255), nullable=False)
    description = db.Column(db.Text)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    disruptions = db.relationship('Disruption', backref='subsystem', lazy=True)

class Category(db.Model):
    __tablename__ = 'categories'
    
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(255), nullable=False)
    description = db.Column(db.Text)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    disruptions = db.relationship('Disruption', backref='category', lazy=True)

class Disruption(db.Model):
    __tablename__ = 'disruptions'
    
    id = db.Column(db.Integer, primary_key=True)
    disruption_number = db.Column(db.String(50), unique=True, nullable=False)
    description = db.Column(db.Text, nullable=False)
    line_id = db.Column(db.Integer, db.ForeignKey('lines.id'), nullable=False)
    sub_system_id = db.Column(db.Integer, db.ForeignKey('subsystems.id'), nullable=False)
    category_id = db.Column(db.Integer, db.ForeignKey('categories.id'))
    duration_minutes = db.Column(db.Integer)
    service_required = db.Column(db.Boolean, default=False)
    start_datetime = db.Column(db.DateTime, nullable=False)
    end_datetime = db.Column(db.DateTime)
    notes = db.Column(db.Text)
    archived = db.Column(db.Boolean, default=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    def generate_disruption_number(self):
        """Generate a unique disruption number based on timestamp and ID"""
        if not self.disruption_number:
            timestamp = datetime.utcnow().strftime('%Y%m%d')
            self.disruption_number = f"DIS-{timestamp}-{self.id or '000'}"
