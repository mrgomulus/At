import sqlite3
import contextlib
import logging

logging.basicConfig(level=logging.INFO)

class Database:
    def __init__(self, db_file):
        self.db_file = db_file

    @contextlib.contextmanager
    def connect(self):
        conn = sqlite3.connect(self.db_file)
        try:
            yield conn
        finally:
            conn.close()

    def execute(self, sql, params=None):
        with self.connect() as conn:
            cursor = conn.cursor()
            cursor.execute(sql, params or [])
            conn.commit()

    def fetch_one(self, sql, params=None):
        with self.connect() as conn:
            cursor = conn.cursor()
            cursor.execute(sql, params or [])
            return cursor.fetchone()

    def fetch_all(self, sql, params=None):
        with self.connect() as conn:
            cursor = conn.cursor()
            cursor.execute(sql, params or [])
            return cursor.fetchall()


def init_db(db_file):
    db = Database(db_file)
    # Example: Create a table if not exists
    db.execute('''CREATE TABLE IF NOT EXISTS example (id INTEGER PRIMARY KEY, name TEXT)''')


def get_db(db_file):
    return Database(db_file)


def close_db(conn):
    conn.close()