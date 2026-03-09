# Configuration Settings for Disruption Management System

class Config:
    DEBUG = False          # Set to True for development mode
    TESTING = False         # Set to True to enable testing features
    DATABASE_URI = 'sqlite:///disruption_management.db'  # Database configuration
    SECRET_KEY = 'your_secret_key_here'    # For session management
    ALLOWED_HOSTS = ['localhost', '127.0.0.1']  # Allowed hosts for the application
    LOGGING_LEVEL = 'INFO'   # Set the logging level

# Configuration for production environment
class ProductionConfig(Config):
    DATABASE_URI = 'mysql://user:password@hostname/db_name'
    DEBUG = False

# Configuration for development environment
class DevelopmentConfig(Config):
    DATABASE_URI = 'sqlite:///disruption_management_dev.db'
    DEBUG = True

# Configuration for testing environment
class TestingConfig(Config):
    DATABASE_URI = 'sqlite:///disruption_management_test.db'
    TESTING = True
    DEBUG = True
