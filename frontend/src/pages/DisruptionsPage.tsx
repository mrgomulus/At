import React, { useEffect, useState } from 'react';
import { getDisruptions, createDisruption, getLines, getSubSystems } from '../api';
import type { Disruption, DisruptionCreate, Line, SubSystem } from '../types';

const DisruptionsPage: React.FC = () => {
  const [disruptions, setDisruptions] = useState<Disruption[]>([]);
  const [lines, setLines] = useState<Line[]>([]);
  const [subSystems, setSubSystems] = useState<SubSystem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  const [formData, setFormData] = useState<DisruptionCreate>({
    description: '',
    line_id: 0,
    sub_system_id: 0,
    duration_minutes: 0,
    service_required: false,
    start_datetime: new Date().toISOString().slice(0, 16),
    notes: '',
  });

  useEffect(() => {
    fetchData();
  }, [page]);

  useEffect(() => {
    const fetchMasterData = async () => {
      try {
        const [linesData, subSystemsData] = await Promise.all([
          getLines(),
          getSubSystems()
        ]);
        setLines(linesData);
        setSubSystems(subSystemsData);
      } catch (err) {
        console.error('Failed to load master data', err);
      }
    };
    fetchMasterData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const data = await getDisruptions(page, 20, false);
      setDisruptions(data.items);
      setTotalPages(data.pages);
      setError(null);
    } catch (err) {
      setError('Failed to load disruptions');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createDisruption(formData);
      setSuccess('Disruption created successfully!');
      setShowForm(false);
      setFormData({
        description: '',
        line_id: 0,
        sub_system_id: 0,
        duration_minutes: 0,
        service_required: false,
        start_datetime: new Date().toISOString().slice(0, 16),
        notes: '',
      });
      fetchData();
      setTimeout(() => setSuccess(null), 3000);
    } catch (err) {
      setError('Failed to create disruption');
      console.error(err);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? (e.target as HTMLInputElement).checked : 
              type === 'number' ? parseInt(value) || 0 : value,
    }));
  };

  if (loading && disruptions.length === 0) {
    return <div className="container"><div className="loading">Loading disruptions...</div></div>;
  }

  const filteredSubSystems = formData.line_id > 0 
    ? subSystems.filter(ss => ss.line_id === formData.line_id)
    : subSystems;

  return (
    <div className="container">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h2>Disruptions</h2>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : 'New Disruption'}
        </button>
      </div>

      {error && <div className="error">{error}</div>}
      {success && <div className="success">{success}</div>}

      {showForm && (
        <div className="card">
          <h3>Create New Disruption</h3>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Description *</label>
              <textarea
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                required
                minLength={10}
                maxLength={2000}
              />
            </div>

            <div className="form-group">
              <label>Line *</label>
              <select
                name="line_id"
                value={formData.line_id}
                onChange={handleInputChange}
                required
              >
                <option value={0}>Select a line</option>
                {lines.map(line => (
                  <option key={line.id} value={line.id}>{line.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>SubSystem *</label>
              <select
                name="sub_system_id"
                value={formData.sub_system_id}
                onChange={handleInputChange}
                required
                disabled={formData.line_id === 0}
              >
                <option value={0}>Select a subsystem</option>
                {filteredSubSystems.map(ss => (
                  <option key={ss.id} value={ss.id}>{ss.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Start Date & Time *</label>
              <input
                type="datetime-local"
                name="start_datetime"
                value={formData.start_datetime}
                onChange={handleInputChange}
                required
              />
            </div>

            <div className="form-group">
              <label>Duration (minutes)</label>
              <input
                type="number"
                name="duration_minutes"
                value={formData.duration_minutes}
                onChange={handleInputChange}
                min={0}
              />
            </div>

            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  name="service_required"
                  checked={formData.service_required}
                  onChange={handleInputChange}
                  style={{ width: 'auto', marginRight: '8px' }}
                />
                Service Required
              </label>
            </div>

            <div className="form-group">
              <label>Notes</label>
              <textarea
                name="notes"
                value={formData.notes}
                onChange={handleInputChange}
              />
            </div>

            <button type="submit" className="btn btn-primary">Create Disruption</button>
          </form>
        </div>
      )}

      <div className="card">
        {disruptions.length === 0 ? (
          <p>No disruptions found. Create your first disruption above!</p>
        ) : (
          <>
            <table>
              <thead>
                <tr>
                  <th>Number</th>
                  <th>Description</th>
                  <th>Start Time</th>
                  <th>Duration</th>
                  <th>Service</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {disruptions.map((disruption) => (
                  <tr key={disruption.id}>
                    <td>{disruption.disruption_number}</td>
                    <td>{disruption.description.substring(0, 80)}...</td>
                    <td>{new Date(disruption.start_datetime).toLocaleString()}</td>
                    <td>{disruption.duration_minutes ? `${disruption.duration_minutes} min` : 'N/A'}</td>
                    <td>{disruption.service_required ? '✓' : '-'}</td>
                    <td>{disruption.end_datetime ? 'Closed' : 'Active'}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {totalPages > 1 && (
              <div style={{ marginTop: '20px', display: 'flex', gap: '10px', justifyContent: 'center' }}>
                <button
                  className="btn btn-secondary"
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                >
                  Previous
                </button>
                <span style={{ padding: '10px' }}>Page {page} of {totalPages}</span>
                <button
                  className="btn btn-secondary"
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                >
                  Next
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default DisruptionsPage;
