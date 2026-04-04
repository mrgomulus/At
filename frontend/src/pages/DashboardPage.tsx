import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getKpis, getDashboardFavorites, getDisruptions, getConnections,
  getDiagnosticSummary,
  type KpiData, type Favorite, type Disruption, type Connection,
  type DiagnosticSummary,
} from '../api/client'

function severityBadge(s: string) {
  const map: Record<string, string> = {
    low: 'badge-info', medium: 'badge-warning', high: 'badge-danger', critical: 'badge-danger',
  }
  return <span className={`badge ${map[s] ?? 'badge-gray'}`}>{s}</span>
}

function statusBadge(status: string) {
  const map: Record<string, string> = {
    connected: 'badge-success', disconnected: 'badge-gray', error: 'badge-danger', connecting: 'badge-warning',
  }
  return <span className={`badge ${map[status] ?? 'badge-gray'}`}>{status}</span>
}

export default function DashboardPage() {
  const [kpis, setKpis] = useState<KpiData | null>(null)
  const [favorites, setFavorites] = useState<Favorite[]>([])
  const [disruptions, setDisruptions] = useState<Disruption[]>([])
  const [connections, setConnections] = useState<Connection[]>([])
  const [diagSummary, setDiagSummary] = useState<DiagnosticSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    Promise.allSettled([
      getKpis(),
      getDashboardFavorites(),
      getDisruptions(),
      getConnections(),
      getDiagnosticSummary(),
    ]).then(([kpiRes, favRes, dispRes, connRes, diagRes]) => {
      if (kpiRes.status === 'fulfilled') setKpis(kpiRes.value)
      if (favRes.status === 'fulfilled') setFavorites(favRes.value)
      if (dispRes.status === 'fulfilled') setDisruptions(dispRes.value)
      if (connRes.status === 'fulfilled') setConnections(connRes.value)
      if (diagRes.status === 'fulfilled') setDiagSummary(diagRes.value)
      setLoading(false)
    }).catch(() => { setError('Failed to load dashboard data'); setLoading(false) })
  }, [])

  if (loading) return <div className="loading">Loading dashboard…</div>

  const recentDisruptions = disruptions.slice(0, 5)
  const connectedCount = connections.filter(c => c.status === 'connected').length

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Dashboard</h2>
          <div className="page-subtitle">System overview and live monitoring</div>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {/* KPI Cards */}
      <div className="kpi-grid">
        <div className="kpi-card">
          <div className="kpi-label">Total Disruptions</div>
          <div className="kpi-value" style={{ color: 'var(--color-danger)' }}>
            {kpis?.total_disruptions ?? disruptions.length}
          </div>
          <div className="kpi-sub">All time</div>
        </div>
        <div className="kpi-card">
          <div className="kpi-label">This Month</div>
          <div className="kpi-value" style={{ color: 'var(--color-warning)' }}>
            {kpis?.disruptions_this_month ?? '—'}
          </div>
          <div className="kpi-sub">Disruptions</div>
        </div>
        <div className="kpi-card">
          <div className="kpi-label">OPC UA Connected</div>
          <div className="kpi-value" style={{ color: 'var(--color-success)' }}>
            {kpis?.connected_opc_ua ?? connectedCount}
          </div>
          <div className="kpi-sub">of {connections.length} connections</div>
        </div>
        <div className="kpi-card">
          <div className="kpi-label">Total Variables</div>
          <div className="kpi-value" style={{ color: 'var(--color-primary)' }}>
            {kpis?.total_variables ?? '—'}
          </div>
          <div className="kpi-sub">Monitored</div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20, marginBottom: 20 }}>
        {/* Dashboard Favorites */}
        <div className="card">
          <div className="card-title">📌 Dashboard Favorites</div>
          {favorites.length === 0 ? (
            <div className="empty-state" style={{ padding: '20px 0' }}>No dashboard favorites yet</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {favorites.map(fav => (
                <div key={fav.id} style={{
                  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                  padding: '8px 12px', background: 'var(--color-surface-2)',
                  borderRadius: 6, border: '1px solid var(--color-border)',
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span
                      className="alarm-indicator"
                      style={{ background: fav.is_alarm_active ? 'var(--color-danger)' : 'var(--color-success)' }}
                    />
                    <span style={{ fontWeight: 500 }}>{fav.custom_label || fav.variable_name || `Fav #${fav.id}`}</span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <span style={{ fontFamily: 'monospace', fontWeight: 600, color: fav.is_alarm_active ? 'var(--color-danger)' : 'var(--color-success)' }}>
                      {fav.current_value !== undefined && fav.current_value !== null ? String(fav.current_value) : '—'}
                    </span>
                    {fav.unit && <span style={{ color: 'var(--color-text-muted)', fontSize: 11 }}>{fav.unit}</span>}
                    {fav.is_alarm_active && <span className="badge badge-danger" style={{ fontSize: 10 }}>ALARM</span>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Connection Health */}
        <div className="card">
          <div className="card-title">🔌 Connection Health</div>
          {connections.length === 0 ? (
            <div className="empty-state" style={{ padding: '20px 0' }}>No connections configured</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {connections.map(conn => (
                <div key={conn.id} style={{
                  display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                  padding: '8px 12px', background: 'var(--color-surface-2)',
                  borderRadius: 6, border: '1px solid var(--color-border)',
                }}>
                  <div>
                    <div style={{ fontWeight: 500 }}>{conn.name}</div>
                    <div style={{ fontSize: 11, color: 'var(--color-text-muted)', marginTop: 2 }}>{conn.url}</div>
                  </div>
                  {statusBadge(conn.status)}
                </div>
              ))}
            </div>
          )}
          <div style={{ marginTop: 12 }}>
            <Link to="/connections" className="btn btn-secondary btn-sm">Manage Connections →</Link>
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
        {/* Recent Disruptions */}
        <div className="card">
          <div className="card-title">⚠️ Recent Disruptions</div>
          {recentDisruptions.length === 0 ? (
            <div className="empty-state" style={{ padding: '20px 0' }}>No disruptions recorded</div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Description</th>
                    <th>Severity</th>
                    <th>Date</th>
                  </tr>
                </thead>
                <tbody>
                  {recentDisruptions.map(d => (
                    <tr key={d.id}>
                      <td className="truncate" style={{ maxWidth: 180 }}>{d.description}</td>
                      <td>{severityBadge(d.severity)}</td>
                      <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>
                        {new Date(d.start_datetime).toLocaleDateString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div style={{ marginTop: 12 }}>
            <Link to="/disruptions" className="btn btn-secondary btn-sm">View All →</Link>
          </div>
        </div>

        {/* Diagnostic Summary */}
        <div className="card">
          <div className="card-title">🔍 Diagnostic Summary</div>
          {diagSummary ? (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              {[
                { label: 'Total Sessions', value: diagSummary.total_sessions, color: 'var(--color-text)' },
                { label: 'Passed', value: diagSummary.passed, color: 'var(--color-success)' },
                { label: 'Failed', value: diagSummary.failed, color: 'var(--color-danger)' },
                { label: 'Warnings', value: diagSummary.warnings, color: 'var(--color-warning)' },
              ].map(item => (
                <div key={item.label} style={{
                  padding: '12px', background: 'var(--color-surface-2)',
                  borderRadius: 6, border: '1px solid var(--color-border)',
                }}>
                  <div style={{ fontSize: 11, color: 'var(--color-text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{item.label}</div>
                  <div style={{ fontSize: 24, fontWeight: 700, color: item.color, marginTop: 4 }}>{item.value}</div>
                </div>
              ))}
              <div style={{
                gridColumn: '1 / -1', padding: '12px', background: 'var(--color-surface-2)',
                borderRadius: 6, border: '1px solid var(--color-border)',
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
              }}>
                <span style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>Pass Rate</span>
                <span style={{
                  fontSize: 18, fontWeight: 700,
                  color: diagSummary.pass_rate >= 80 ? 'var(--color-success)' : diagSummary.pass_rate >= 60 ? 'var(--color-warning)' : 'var(--color-danger)',
                }}>
                  {diagSummary.pass_rate.toFixed(1)}%
                </span>
              </div>
            </div>
          ) : (
            <div className="empty-state" style={{ padding: '20px 0' }}>No diagnostic data available</div>
          )}
          <div style={{ marginTop: 12 }}>
            <Link to="/diagnostics" className="btn btn-secondary btn-sm">Run Diagnostics →</Link>
          </div>
        </div>
      </div>
    </div>
  )
}
