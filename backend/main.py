from app import create_app
from app.database import engine, Base
from app import models  # noqa: F401 - ensure all models are registered

Base.metadata.create_all(bind=engine)

app = create_app()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=5000, reload=True)
