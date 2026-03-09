-- OPC UA Variables and Tracking Tables

CREATE TABLE opc_ua_variables (
    id SERIAL PRIMARY KEY,
    node_id VARCHAR(255) UNIQUE NOT NULL,
    variable_name VARCHAR(255) NOT NULL,
    data_type VARCHAR(50) NOT NULL,
    value TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE opc_ua_tracking (
    id SERIAL PRIMARY KEY,
    node_id VARCHAR(255) NOT NULL,
    tracking_timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    old_value TEXT,
    new_value TEXT,
    FOREIGN KEY (node_id) REFERENCES opc_ua_variables(node_id)
);