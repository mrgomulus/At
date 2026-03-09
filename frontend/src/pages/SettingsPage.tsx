import React, { useEffect, useState } from 'react';
import { getLines, createLine, getSubSystems, createSubSystem } from '../api';
import type { Line, LineCreate, SubSystem, SubSystemCreate } from '../types';

const SettingsPage: React.FC = () => {
  const [lines, setLines] = useState<Line[]>([]);
  const [subSystems, setSubSystems] = useState<SubSystem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  const [newLine, setNewLine] = useState<LineCreate>({ name: '', description: '' });
  const [newSubSystem, setNewSubSystem] = useState<SubSystemCreate>({ 
    line_id: 0, 
    name: '', 
    description: '' 
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      const [linesData, subSystemsData] = await Promise.all([
        getLines(),
        getSubSystems()
      ]);
      setLines(linesData);
      setSubSystems(subSystemsData);
    } catch (err) {
      setError('Failed to load data');
      console.error(err);
    }
  };

  const handleCreateLine = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createLine(newLine);
      setSuccess('Line created successfully!');
      setNewLine({ name: '', description: '' });
      fetchData();
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to create line');
      console.error(err);
    }
  };

  const handleCreateSubSystem = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createSubSystem(newSubSystem);
      setSuccess('SubSystem created successfully!');
      setNewSubSystem({ line_id: 0, name: '', description: '' });
      fetchData();
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to create subsystem');
      console.error(err);
    }
  };

  return (
    <div className="container">
      <h2>Settings</h2>
      
      {error && <div className="error">{error}</div>}
      {success && <div className="success">{success}</div>}

      <div className="card">
        <h3>Production Lines</h3>
        
        <form onSubmit={handleCreateLine} style={{ marginBottom: '20px' }}>
          <div className="form-group">
            <label>Line Name *</label>
            <input
              type="text"
              value={newLine.name}
              onChange={(e) => setNewLine({ ...newLine, name: e.target.value })}
              required
            />
          </div>
          <div className="form-group">
            <label>Description</label>
            <input
              type="text"
              value={newLine.description || ''}
              onChange={(e) => setNewLine({ ...newLine, description: e.target.value })}
            />
          </div>
          <button type="submit" className="btn btn-primary">Add Line</button>
        </form>

        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Name</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line) => (
              <tr key={line.id}>
                <td>{line.id}</td>
                <td>{line.name}</td>
                <td>{line.description || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>SubSystems</h3>
        
        <form onSubmit={handleCreateSubSystem} style={{ marginBottom: '20px' }}>
          <div className="form-group">
            <label>Line *</label>
            <select
              value={newSubSystem.line_id}
              onChange={(e) => setNewSubSystem({ ...newSubSystem, line_id: parseInt(e.target.value) })}
              required
            >
              <option value={0}>Select a line</option>
              {lines.map((line) => (
                <option key={line.id} value={line.id}>{line.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>SubSystem Name *</label>
            <input
              type="text"
              value={newSubSystem.name}
              onChange={(e) => setNewSubSystem({ ...newSubSystem, name: e.target.value })}
              required
            />
          </div>
          <div className="form-group">
            <label>Description</label>
            <input
              type="text"
              value={newSubSystem.description || ''}
              onChange={(e) => setNewSubSystem({ ...newSubSystem, description: e.target.value })}
            />
          </div>
          <button type="submit" className="btn btn-primary">Add SubSystem</button>
        </form>

        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Line</th>
              <th>Name</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            {subSystems.map((ss) => {
              const line = lines.find(l => l.id === ss.line_id);
              return (
                <tr key={ss.id}>
                  <td>{ss.id}</td>
                  <td>{line?.name || 'Unknown'}</td>
                  <td>{ss.name}</td>
                  <td>{ss.description || '-'}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default SettingsPage;