"""
Basic tests for the Disruption Management System API
"""
import pytest
from app import create_app, db
from app.models import Line, SubSystem, Disruption
from datetime import datetime

@pytest.fixture
def app():
    """Create and configure a test app instance."""
    app = create_app('testing')
    
    with app.app_context():
        db.create_all()
        yield app
        db.drop_all()

@pytest.fixture
def client(app):
    """Create a test client for the app."""
    return app.test_client()

def test_health_check(client):
    """Test the health check endpoint."""
    response = client.get('/api/health')
    assert response.status_code == 200
    data = response.get_json()
    assert data['status'] == 'healthy'
    assert 'timestamp' in data

def test_create_line(client):
    """Test creating a production line."""
    response = client.post('/api/lines', json={
        'name': 'Test Line',
        'description': 'A test production line'
    })
    assert response.status_code == 201
    data = response.get_json()
    assert data['name'] == 'Test Line'
    assert data['description'] == 'A test production line'
    assert 'id' in data

def test_get_lines(client):
    """Test getting all lines."""
    # Create a line first
    client.post('/api/lines', json={
        'name': 'Test Line',
        'description': 'A test line'
    })
    
    response = client.get('/api/lines')
    assert response.status_code == 200
    data = response.get_json()
    assert isinstance(data, list)
    assert len(data) == 1
    assert data[0]['name'] == 'Test Line'

def test_create_subsystem(client):
    """Test creating a subsystem."""
    # First create a line
    line_response = client.post('/api/lines', json={
        'name': 'Test Line'
    })
    line_id = line_response.get_json()['id']
    
    # Then create a subsystem
    response = client.post('/api/subsystems', json={
        'line_id': line_id,
        'name': 'Test SubSystem',
        'description': 'A test subsystem'
    })
    assert response.status_code == 201
    data = response.get_json()
    assert data['name'] == 'Test SubSystem'
    assert data['line_id'] == line_id

def test_create_disruption(client):
    """Test creating a disruption."""
    # Create line and subsystem first
    line_response = client.post('/api/lines', json={'name': 'Test Line'})
    line_id = line_response.get_json()['id']
    
    subsystem_response = client.post('/api/subsystems', json={
        'line_id': line_id,
        'name': 'Test SubSystem'
    })
    subsystem_id = subsystem_response.get_json()['id']
    
    # Create disruption
    response = client.post('/api/disruptions', json={
        'description': 'Test disruption with enough characters to meet minimum',
        'line_id': line_id,
        'sub_system_id': subsystem_id,
        'duration_minutes': 30,
        'service_required': True,
        'start_datetime': '2026-03-09T10:00:00'
    })
    assert response.status_code == 201
    data = response.get_json()
    assert 'disruption_number' in data
    assert data['duration_minutes'] == 30
    assert data['service_required'] is True

def test_get_kpis(client):
    """Test getting KPIs."""
    response = client.get('/api/kpis')
    assert response.status_code == 200
    data = response.get_json()
    assert 'total_disruptions' in data
    assert 'disruptions_this_month' in data
    assert 'average_duration_hours' in data
    assert 'trend' in data

def test_get_disruptions_paginated(client):
    """Test getting disruptions with pagination."""
    response = client.get('/api/disruptions?page=1&per_page=10')
    assert response.status_code == 200
    data = response.get_json()
    assert 'items' in data
    assert 'total' in data
    assert 'page' in data
    assert 'per_page' in data
    assert 'pages' in data

if __name__ == '__main__':
    pytest.main([__file__, '-v'])
