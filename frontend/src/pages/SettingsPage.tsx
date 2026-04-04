import { useEffect, useState } from 'react'
import { getKpiHealth } from '../api/client'

interface HealthData {
  status?: string
  database?: string
  uptime?: number | string
  version?: string
  [key: string]: unknown
}

const API_BASE_KEY = 'opc_ua_api_base'
const DEFAULT_BASE = 'http://localhost:5000/api'

export default function SettingsPage() {
  const [apiBase, setApiBase] = useState(() => localStorage.getItem(API_BASE_KEY) || DEFAULT_BASE)
  const [saved, setSaved] = useState(false)
  const [health, setHealth] = useState<HealthData | null>(null)
  const [healthLoading, setHealthLoading] = useState(false)
  const [healthError, setHealthError] = useState('')

  const handleSaveApiBase = () => {
    localStorage.setItem(API_BASE_KEY, apiBase)
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  const checkHealth = () => {
    setHealthLoading(true)
    setHealthError('')
    getKpiHealth()
      .then(data => { setHealth(data as HealthData); setHealthLoading(false) })
      .catch(() => { setHealthError('Cannot reach backend. Check the URL and make sure the server is running.'); setHealthLoading(false) })
  }

  useEffect(() => { checkHealth() }, [])

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Settings</h2>
          <div className="page-subtitle">Application configuration</div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
        {/* Backend Connection */}
        <div className="card">
          <div className="card-title">🔗 Backend Connection</div>
          <div className="form-group">
            <label className="form-label">API Base URL</label>
            <input
              className="form-control"
              value={apiBase}
              onChange={e => setApiBase(e.target.value)}
              placeholder="http://localhost:5000/api"
            />
            <div style={{ fontSize: 11, color: 'var(--color-text-muted)', marginTop: 6 }}>
              The base URL used for all API requests. Requires page refresh to take effect.
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn btn-primary" onClick={handleSaveApiBase}>
              {saved ? '✓ Saved' : 'Save URL'}
            </button>
            <button className="btn btn-secondary" onClick={() => setApiBase(DEFAULT_BASE)}>
              Reset to Default
            </button>
          </div>
        </div>

        {/* Backend Health */}
        <div className="card">
          <div className="card-title">💊 Backend Health</div>
          <div style={{ marginBottom: 14 }}>
            <button className="btn btn-secondary" onClick={checkHealth} disabled={healthLoading}>
              {healthLoading ? '⏳ Checking…' : '🔄 Check Health'}
            </button>
          </div>
          {healthError && <div className="error-msg">{healthError}</div>}
          {health && !healthError && (
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 12 }}>
                <span className={`badge ${health.status === 'ok' || health.status === 'healthy' ? 'badge-success' : 'badge-danger'}`}>
                  {health.status === 'ok' || health.status === 'healthy' ? '✓ Healthy' : `⚠ ${health.status ?? 'Unknown'}`}
                </span>
              </div>
              <div className="info-grid">
                {Object.entries(health).map(([k, v]) => (
                  <div key={k} style={{ display: 'contents' }}>
                    <div className="info-label">{k}</div>
                    <div className="info-value">{String(v)}</div>
                  </div>
                ))}
              </div>
            </div>
          )}
          {!health && !healthError && !healthLoading && (
            <div className="empty-state" style={{ padding: '20px 0' }}>No health data yet.</div>
          )}
        </div>
      </div>

      {/* App Info */}
      <div className="card" style={{ marginTop: 20 }}>
        <div className="card-title">ℹ️ Application Info</div>
        <div className="info-grid">
          <div className="info-label">Application</div>
          <div className="info-value">OPC UA Monitor — Disruption Management System</div>
          <div className="info-label">Version</div>
          <div className="info-value">2.0.0</div>
          <div className="info-label">Frontend</div>
          <div className="info-value">React 18 + TypeScript + Vite</div>
          <div className="info-label">API Protocol</div>
          <div className="info-value">REST / HTTP (OPC UA via backend)</div>
          <div className="info-label">Description</div>
          <div className="info-value" style={{ fontFamily: 'inherit' }}>
            Industrial monitoring frontend for OPC UA data acquisition, disruption tracking,
            and real-time diagnostics.
          </div>
        </div>
      </div>

      {/* Quick Links */}
      <div className="card" style={{ marginTop: 20 }}>
        <div className="card-title">🔗 API Endpoints Reference</div>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: 8 }}>
          {[
            '/api/connections/', '/api/variables/', '/api/favorites/',
            '/api/favorites/dashboard/all', '/api/diagnostics/sessions',
            '/api/diagnostics/summary', '/api/disruptions/', '/api/kpis/',
            '/api/kpis/health',
          ].map(ep => (
            <div key={ep} style={{
              padding: '6px 12px', background: 'var(--color-surface-2)',
              borderRadius: 4, fontFamily: 'monospace', fontSize: 12,
              color: 'var(--color-primary)', border: '1px solid var(--color-border)',
            }}>
              {ep}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
