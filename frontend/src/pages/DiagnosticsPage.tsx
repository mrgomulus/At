import { useEffect, useState } from 'react'
import {
  getConnections, getDiagnosticSessions, getDiagnosticSummary, runDiagnostic,
  type Connection, type DiagnosticSession, type DiagnosticSummary,
} from '../api/client'

function diagStatusBadge(s: string) {
  const m: Record<string, string> = { passed: 'badge-success', failed: 'badge-danger', warning: 'badge-warning', running: 'badge-info' }
  return <span className={`badge ${m[s] ?? 'badge-gray'}`}>{s}</span>
}

type SessionType = 'health_check' | 'connection_test' | 'full_scan'

export default function DiagnosticsPage() {
  const [connections, setConnections] = useState<Connection[]>([])
  const [sessions, setSessions] = useState<DiagnosticSession[]>([])
  const [summary, setSummary] = useState<DiagnosticSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [runningId, setRunningId] = useState<number | null>(null)
  const [diagType, setDiagType] = useState<SessionType>('health_check')

  const load = () => {
    setLoading(true)
    Promise.allSettled([
      getConnections(),
      getDiagnosticSessions(),
      getDiagnosticSummary(),
    ]).then(([connRes, sessRes, sumRes]) => {
      if (connRes.status === 'fulfilled') setConnections(connRes.value)
      if (sessRes.status === 'fulfilled') setSessions(sessRes.value)
      if (sumRes.status === 'fulfilled') setSummary(sumRes.value)
      setLoading(false)
    }).catch(() => { setError('Failed to load diagnostics'); setLoading(false) })
  }

  useEffect(() => { load() }, [])

  const handleRun = async (connId: number) => {
    setRunningId(connId)
    try {
      await runDiagnostic(connId, diagType)
      load()
    } catch {
      setError(`Failed to run diagnostic on connection ${connId}`)
    } finally {
      setRunningId(null)
    }
  }

  if (loading) return <div className="loading">Loading diagnostics…</div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Diagnostics</h2>
          <div className="page-subtitle">System health monitoring and analysis</div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <label style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>Type:</label>
          <select className="form-control" style={{ maxWidth: 180 }} value={diagType}
            onChange={e => setDiagType(e.target.value as SessionType)}>
            <option value="health_check">Health Check</option>
            <option value="connection_test">Connection Test</option>
            <option value="full_scan">Full Scan</option>
          </select>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {/* Summary KPIs */}
      {summary && (
        <div className="kpi-grid" style={{ marginBottom: 24 }}>
          <div className="kpi-card">
            <div className="kpi-label">Total Sessions</div>
            <div className="kpi-value">{summary.total_sessions}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Passed</div>
            <div className="kpi-value" style={{ color: 'var(--color-success)' }}>{summary.passed}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Failed</div>
            <div className="kpi-value" style={{ color: 'var(--color-danger)' }}>{summary.failed}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Warnings</div>
            <div className="kpi-value" style={{ color: 'var(--color-warning)' }}>{summary.warnings}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">Pass Rate</div>
            <div className="kpi-value" style={{
              color: summary.pass_rate >= 80 ? 'var(--color-success)'
                : summary.pass_rate >= 60 ? 'var(--color-warning)'
                : 'var(--color-danger)',
            }}>
              {summary.pass_rate.toFixed(1)}%
            </div>
          </div>
        </div>
      )}

      {/* Connections – run diagnostic per connection */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div className="section-title">Run Diagnostic per Connection</div>
        {connections.length === 0 ? (
          <div className="empty-state">No connections available.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Connection</th>
                  <th>URL</th>
                  <th>Status</th>
                  <th style={{ textAlign: 'right' }}>Action</th>
                </tr>
              </thead>
              <tbody>
                {connections.map(conn => (
                  <tr key={conn.id}>
                    <td style={{ fontWeight: 600 }}>{conn.name}</td>
                    <td style={{ fontFamily: 'monospace', fontSize: 12, color: 'var(--color-text-muted)' }}>{conn.url}</td>
                    <td>
                      <span className={`badge ${conn.status === 'connected' ? 'badge-success' : conn.status === 'error' ? 'badge-danger' : 'badge-gray'}`}>
                        {conn.status}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() => handleRun(conn.id)}
                        disabled={runningId === conn.id}
                      >
                        {runningId === conn.id ? '⏳ Running…' : '▶ Run'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* All Sessions */}
      <div className="card">
        <div className="section-title">Recent Sessions ({sessions.length})</div>
        {sessions.length === 0 ? (
          <div className="empty-state">No diagnostic sessions yet.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Connection</th>
                  <th>Type</th>
                  <th>Status</th>
                  <th>Result</th>
                  <th>Duration</th>
                  <th>Started</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map(s => {
                  const conn = connections.find(c => c.id === s.connection_id)
                  return (
                    <tr key={s.id}>
                      <td style={{ fontWeight: 500 }}>{s.connection_name || conn?.name || `#${s.connection_id}`}</td>
                      <td style={{ fontFamily: 'monospace', fontSize: 12 }}>{s.session_type}</td>
                      <td>{diagStatusBadge(s.status)}</td>
                      <td style={{ fontSize: 12, color: 'var(--color-text-muted)', maxWidth: 220 }} className="truncate">{s.result || '—'}</td>
                      <td style={{ fontSize: 12, fontFamily: 'monospace' }}>{s.duration_ms != null ? `${s.duration_ms}ms` : '—'}</td>
                      <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{new Date(s.started_at).toLocaleString()}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
