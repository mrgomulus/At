"""
Initialize the database with sample data for testing/demonstration
"""
from app import create_app, db
from app.models import Line, SubSystem, Category, Disruption
from datetime import datetime, timedelta

def init_sample_data():
    app = create_app('development')
    
    with app.app_context():
        # Clear existing data
        Disruption.query.delete()
        SubSystem.query.delete()
        Line.query.delete()
        Category.query.delete()
        
        # Create Lines
        line1 = Line(name="Assembly Line A", description="Main assembly line for Product A")
        line2 = Line(name="Assembly Line B", description="Secondary assembly line for Product B")
        line3 = Line(name="Packaging Line", description="Automated packaging line")
        
        db.session.add_all([line1, line2, line3])
        db.session.commit()
        
        # Create SubSystems
        subsystems = [
            SubSystem(line_id=line1.id, name="Conveyor Belt 1", description="Primary conveyor system"),
            SubSystem(line_id=line1.id, name="Robotic Arm", description="Assembly robot"),
            SubSystem(line_id=line1.id, name="Quality Scanner", description="Vision inspection system"),
            SubSystem(line_id=line2.id, name="Conveyor Belt 2", description="Secondary conveyor"),
            SubSystem(line_id=line2.id, name="Welding Station", description="Automated welding"),
            SubSystem(line_id=line3.id, name="Boxing Machine", description="Automated boxing"),
            SubSystem(line_id=line3.id, name="Label Printer", description="Product labeling"),
        ]
        
        db.session.add_all(subsystems)
        db.session.commit()
        
        # Create Categories
        categories = [
            Category(name="Mechanical Failure", description="Hardware or mechanical issues"),
            Category(name="Software Error", description="Software or control system errors"),
            Category(name="Power Outage", description="Electrical power disruptions"),
            Category(name="Maintenance", description="Scheduled or unscheduled maintenance"),
        ]
        
        db.session.add_all(categories)
        db.session.commit()
        
        # Create Sample Disruptions
        disruptions = [
            Disruption(
                disruption_number="DIS-20260309-001",
                description="Conveyor belt motor overheated and shut down. Replaced cooling fan.",
                line_id=line1.id,
                sub_system_id=subsystems[0].id,
                category_id=categories[0].id,
                duration_minutes=120,
                service_required=True,
                start_datetime=datetime.utcnow() - timedelta(days=5, hours=3),
                end_datetime=datetime.utcnow() - timedelta(days=5, hours=1),
                notes="Cooling system needs regular inspection"
            ),
            Disruption(
                disruption_number="DIS-20260309-002",
                description="PLC software error caused robotic arm to freeze. System restarted.",
                line_id=line1.id,
                sub_system_id=subsystems[1].id,
                category_id=categories[1].id,
                duration_minutes=30,
                service_required=False,
                start_datetime=datetime.utcnow() - timedelta(days=3, hours=7),
                end_datetime=datetime.utcnow() - timedelta(days=3, hours=6, minutes=30),
                notes="Software update scheduled"
            ),
            Disruption(
                disruption_number="DIS-20260309-003",
                description="Building-wide power outage affected all production lines.",
                line_id=line2.id,
                sub_system_id=subsystems[3].id,
                category_id=categories[2].id,
                duration_minutes=90,
                service_required=False,
                start_datetime=datetime.utcnow() - timedelta(days=2, hours=9),
                end_datetime=datetime.utcnow() - timedelta(days=2, hours=7, minutes=30),
            ),
            Disruption(
                disruption_number="DIS-20260309-004",
                description="Scheduled maintenance on welding station.",
                line_id=line2.id,
                sub_system_id=subsystems[4].id,
                category_id=categories[3].id,
                duration_minutes=180,
                service_required=True,
                start_datetime=datetime.utcnow() - timedelta(days=1, hours=2),
                end_datetime=datetime.utcnow() - timedelta(hours=23),
                notes="Routine maintenance completed successfully"
            ),
            Disruption(
                disruption_number="DIS-20260309-005",
                description="Label printer ran out of labels, production stopped.",
                line_id=line3.id,
                sub_system_id=subsystems[6].id,
                category_id=categories[0].id,
                duration_minutes=15,
                service_required=False,
                start_datetime=datetime.utcnow() - timedelta(hours=4),
                end_datetime=datetime.utcnow() - timedelta(hours=3, minutes=45),
                notes="Restocked labels, improved monitoring"
            ),
        ]
        
        db.session.add_all(disruptions)
        db.session.commit()
        
        print("✅ Sample data initialized successfully!")
        print(f"   - Created {len([line1, line2, line3])} lines")
        print(f"   - Created {len(subsystems)} subsystems")
        print(f"   - Created {len(categories)} categories")
        print(f"   - Created {len(disruptions)} disruptions")

if __name__ == "__main__":
    init_sample_data()
