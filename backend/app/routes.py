from flask import Blueprint, request, jsonify
from app.models import db, Disruption, Line, SubSystem, Category
from app.schemas import (
    DisruptionCreate, DisruptionResponse,
    LineCreate, LineResponse,
    SubSystemCreate, SubSystemResponse,
    KPIResponse
)
from datetime import datetime, timedelta
from sqlalchemy import func

api_bp = Blueprint('api', __name__)

# Health check endpoint
@api_bp.route('/health', methods=['GET'])
def health_check():
    return jsonify({'status': 'healthy', 'timestamp': datetime.utcnow().isoformat()})

# Line endpoints
@api_bp.route('/lines', methods=['GET'])
def get_lines():
    lines = Line.query.all()
    return jsonify([{
        'id': line.id,
        'name': line.name,
        'description': line.description,
        'created_at': line.created_at.isoformat(),
        'updated_at': line.updated_at.isoformat()
    } for line in lines])

@api_bp.route('/lines', methods=['POST'])
def create_line():
    try:
        data = request.get_json()
        line_data = LineCreate(**data)
        
        line = Line(
            name=line_data.name,
            description=line_data.description
        )
        db.session.add(line)
        db.session.commit()
        
        return jsonify({
            'id': line.id,
            'name': line.name,
            'description': line.description,
            'created_at': line.created_at.isoformat(),
            'updated_at': line.updated_at.isoformat()
        }), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 400

@api_bp.route('/lines/<int:line_id>', methods=['GET'])
def get_line(line_id):
    line = Line.query.get_or_404(line_id)
    return jsonify({
        'id': line.id,
        'name': line.name,
        'description': line.description,
        'created_at': line.created_at.isoformat(),
        'updated_at': line.updated_at.isoformat()
    })

# SubSystem endpoints
@api_bp.route('/subsystems', methods=['GET'])
def get_subsystems():
    line_id = request.args.get('line_id', type=int)
    query = SubSystem.query
    if line_id:
        query = query.filter_by(line_id=line_id)
    subsystems = query.all()
    
    return jsonify([{
        'id': ss.id,
        'line_id': ss.line_id,
        'name': ss.name,
        'description': ss.description,
        'created_at': ss.created_at.isoformat(),
        'updated_at': ss.updated_at.isoformat()
    } for ss in subsystems])

@api_bp.route('/subsystems', methods=['POST'])
def create_subsystem():
    try:
        data = request.get_json()
        subsystem_data = SubSystemCreate(**data)
        
        subsystem = SubSystem(
            line_id=subsystem_data.line_id,
            name=subsystem_data.name,
            description=subsystem_data.description
        )
        db.session.add(subsystem)
        db.session.commit()
        
        return jsonify({
            'id': subsystem.id,
            'line_id': subsystem.line_id,
            'name': subsystem.name,
            'description': subsystem.description,
            'created_at': subsystem.created_at.isoformat(),
            'updated_at': subsystem.updated_at.isoformat()
        }), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 400

# Disruption endpoints
@api_bp.route('/disruptions', methods=['GET'])
def get_disruptions():
    page = request.args.get('page', 1, type=int)
    per_page = request.args.get('per_page', 20, type=int)
    archived = request.args.get('archived', 'false').lower() == 'true'
    
    query = Disruption.query.filter_by(archived=archived).order_by(Disruption.start_datetime.desc())
    pagination = query.paginate(page=page, per_page=per_page, error_out=False)
    
    return jsonify({
        'items': [{
            'id': d.id,
            'disruption_number': d.disruption_number,
            'description': d.description,
            'line_id': d.line_id,
            'sub_system_id': d.sub_system_id,
            'category_id': d.category_id,
            'duration_minutes': d.duration_minutes,
            'service_required': d.service_required,
            'start_datetime': d.start_datetime.isoformat(),
            'end_datetime': d.end_datetime.isoformat() if d.end_datetime else None,
            'notes': d.notes,
            'archived': d.archived,
            'created_at': d.created_at.isoformat(),
            'updated_at': d.updated_at.isoformat()
        } for d in pagination.items],
        'total': pagination.total,
        'page': pagination.page,
        'per_page': pagination.per_page,
        'pages': pagination.pages
    })

@api_bp.route('/disruptions', methods=['POST'])
def create_disruption():
    try:
        data = request.get_json()
        disruption_data = DisruptionCreate(**data)
        
        # Generate unique disruption number
        timestamp = datetime.utcnow().strftime('%Y%m%d%H%M%S')
        disruption_number = f"DIS-{timestamp}"
        
        disruption = Disruption(
            disruption_number=disruption_number,
            description=disruption_data.description,
            line_id=disruption_data.line_id,
            sub_system_id=disruption_data.sub_system_id,
            category_id=disruption_data.category_id,
            duration_minutes=disruption_data.duration_minutes,
            service_required=disruption_data.service_required,
            start_datetime=disruption_data.start_datetime,
            end_datetime=disruption_data.end_datetime,
            notes=disruption_data.notes
        )
        db.session.add(disruption)
        db.session.commit()
        
        return jsonify({
            'id': disruption.id,
            'disruption_number': disruption.disruption_number,
            'description': disruption.description,
            'line_id': disruption.line_id,
            'sub_system_id': disruption.sub_system_id,
            'category_id': disruption.category_id,
            'duration_minutes': disruption.duration_minutes,
            'service_required': disruption.service_required,
            'start_datetime': disruption.start_datetime.isoformat(),
            'end_datetime': disruption.end_datetime.isoformat() if disruption.end_datetime else None,
            'notes': disruption.notes,
            'archived': disruption.archived,
            'created_at': disruption.created_at.isoformat(),
            'updated_at': disruption.updated_at.isoformat()
        }), 201
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 400

@api_bp.route('/disruptions/<int:disruption_id>', methods=['GET'])
def get_disruption(disruption_id):
    disruption = Disruption.query.get_or_404(disruption_id)
    return jsonify({
        'id': disruption.id,
        'disruption_number': disruption.disruption_number,
        'description': disruption.description,
        'line_id': disruption.line_id,
        'sub_system_id': disruption.sub_system_id,
        'category_id': disruption.category_id,
        'duration_minutes': disruption.duration_minutes,
        'service_required': disruption.service_required,
        'start_datetime': disruption.start_datetime.isoformat(),
        'end_datetime': disruption.end_datetime.isoformat() if disruption.end_datetime else None,
        'notes': disruption.notes,
        'archived': disruption.archived,
        'created_at': disruption.created_at.isoformat(),
        'updated_at': disruption.updated_at.isoformat()
    })

@api_bp.route('/disruptions/<int:disruption_id>', methods=['PUT'])
def update_disruption(disruption_id):
    try:
        disruption = Disruption.query.get_or_404(disruption_id)
        data = request.get_json()
        
        # Update fields
        if 'description' in data:
            disruption.description = data['description']
        if 'duration_minutes' in data:
            disruption.duration_minutes = data['duration_minutes']
        if 'service_required' in data:
            disruption.service_required = data['service_required']
        if 'end_datetime' in data:
            disruption.end_datetime = datetime.fromisoformat(data['end_datetime']) if data['end_datetime'] else None
        if 'notes' in data:
            disruption.notes = data['notes']
        if 'archived' in data:
            disruption.archived = data['archived']
        
        db.session.commit()
        
        return jsonify({
            'id': disruption.id,
            'disruption_number': disruption.disruption_number,
            'description': disruption.description,
            'line_id': disruption.line_id,
            'sub_system_id': disruption.sub_system_id,
            'category_id': disruption.category_id,
            'duration_minutes': disruption.duration_minutes,
            'service_required': disruption.service_required,
            'start_datetime': disruption.start_datetime.isoformat(),
            'end_datetime': disruption.end_datetime.isoformat() if disruption.end_datetime else None,
            'notes': disruption.notes,
            'archived': disruption.archived,
            'created_at': disruption.created_at.isoformat(),
            'updated_at': disruption.updated_at.isoformat()
        })
    except Exception as e:
        db.session.rollback()
        return jsonify({'error': str(e)}), 400

# KPI endpoint
@api_bp.route('/kpis', methods=['GET'])
def get_kpis():
    try:
        # Total disruptions
        total_disruptions = Disruption.query.filter_by(archived=False).count()
        
        # Disruptions this month
        start_of_month = datetime.utcnow().replace(day=1, hour=0, minute=0, second=0, microsecond=0)
        disruptions_this_month = Disruption.query.filter(
            Disruption.start_datetime >= start_of_month,
            Disruption.archived == False
        ).count()
        
        # Total duration in hours
        total_duration = db.session.query(
            func.sum(Disruption.duration_minutes)
        ).filter(Disruption.archived == False).scalar() or 0
        total_duration_hours = total_duration / 60.0
        
        # Average duration
        avg_duration = total_duration_hours / total_disruptions if total_disruptions > 0 else 0
        
        # Service required count
        with_service = Disruption.query.filter_by(service_required=True, archived=False).count()
        service_percentage = (with_service / total_disruptions * 100) if total_disruptions > 0 else 0
        
        # Calculate trend (compare this month to last month)
        last_month_start = (start_of_month - timedelta(days=1)).replace(day=1)
        disruptions_last_month = Disruption.query.filter(
            Disruption.start_datetime >= last_month_start,
            Disruption.start_datetime < start_of_month
        ).count()
        
        if disruptions_last_month == 0:
            trend = 'stable'
        elif disruptions_this_month > disruptions_last_month:
            trend = 'increasing'
        elif disruptions_this_month < disruptions_last_month:
            trend = 'decreasing'
        else:
            trend = 'stable'
        
        return jsonify({
            'total_disruptions': total_disruptions,
            'disruptions_this_month': disruptions_this_month,
            'total_duration_hours': round(total_duration_hours, 2),
            'average_duration_hours': round(avg_duration, 2),
            'with_service_required': with_service,
            'service_percentage': round(service_percentage, 2),
            'trend': trend
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500
