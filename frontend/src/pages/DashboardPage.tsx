import React, { useEffect, useState } from 'react';
import { getKPIs, getDisruptions } from '../api';
import type { KPI, Disruption } from '../types';

const DashboardPage: React.FC = () => {
  const [kpis, setKpis] = useState<KPI | null>(null);
  const [recentDisruptions, setRecentDisruptions] = useState<Disruption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const [kpiData, disruptionsData] = await Promise.all([
          getKPIs(),
          getDisruptions(1, 5, false)
        ]);
        setKpis(kpiData);
        setRecentDisruptions(disruptionsData.items);
        setError(null);
      } catch (err) {
        setError('Failed to load dashboard data');
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return <div className="container"><div className="loading">Loading dashboard...</div></div>;
  }

  if (error) {
    return <div className="container"><div className="error">{error}</div></div>;
  }

  return (
    <div className="container">
      <h2>Dashboard</h2>
      
      {kpis && (
        <div className="kpi-grid">
          <div className="kpi-card">
            <h3>Total Disruptions</h3>
            <div className="value">{kpis.total_disruptions}</div>
          </div>
          <div className="kpi-card">
            <h3>This Month</h3>
            <div className="value">{kpis.disruptions_this_month}</div>
          </div>
          <div className="kpi-card">
            <h3>Total Duration</h3>
            <div className="value">{kpis.total_duration_hours}h</div>
          </div>
          <div className="kpi-card">
            <h3>Avg Duration</h3>
            <div className="value">{kpis.average_duration_hours}h</div>
          </div>
          <div className="kpi-card">
            <h3>Service Required</h3>
            <div className="value">{kpis.service_percentage}%</div>
          </div>
          <div className="kpi-card">
            <h3>Trend</h3>
            <div className="value" style={{ fontSize: '20px' }}>
              {kpis.trend === 'increasing' && '📈 Increasing'}
              {kpis.trend === 'decreasing' && '📉 Decreasing'}
              {kpis.trend === 'stable' && '➡️ Stable'}
            </div>
          </div>
        </div>
      )}

      <div className="card">
        <h3>Recent Disruptions</h3>
        {recentDisruptions.length === 0 ? (
          <p>No disruptions recorded yet.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Number</th>
                <th>Description</th>
                <th>Start Time</th>
                <th>Duration</th>
                <th>Service</th>
              </tr>
            </thead>
            <tbody>
              {recentDisruptions.map((disruption) => (
                <tr key={disruption.id}>
                  <td>{disruption.disruption_number}</td>
                  <td>{disruption.description.substring(0, 50)}...</td>
                  <td>{new Date(disruption.start_datetime).toLocaleString()}</td>
                  <td>{disruption.duration_minutes ? `${disruption.duration_minutes} min` : 'N/A'}</td>
                  <td>{disruption.service_required ? '✓' : '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default DashboardPage;
