import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  getConnections, getConnectionStats, testConnection,
  getVariables, createVariable, deleteVariable,
  getFavorites, createFavorite, updateFavorite, deleteFavorite,
  getDiagnosticSessions, runDiagnostic,
  type Connection, type ConnectionStats, type Variable,
  type Favorite, type DiagnosticSession,
} from '../api/client'

type Tab = 'overview' | 'variables' | 'favorites' | 'diagnostics'
type SessionType = 'connection_test' | 'health_check' | 'full_scan'

interface VarForm { name: string; node_id: string; data_type: string; description: string }
interface FavEditState { id: number; custom_label: string; unit: string; color: string; alarm_min: string; alarm_max: string; alarm_enabled: boolean }

const emptyVarForm: VarForm = { name: '', node_id: '', data_type: 'Float', description: '' }

function statusBadge(s: string) {
  const m: Record<string, string> = { connected: 'badge-success', disconnected: 'badge-gray', error: 'badge-danger', connecting: 'badge-warning' }
  return <span className={`badge ${m[s] ?? 'badge-gray'}`}>{s}</span>
}
function diagStatusBadge(s: string) {
  const m: Record<string, string> = { passed: 'badge-success', failed: 'badge-danger', warning: 'badge-warning', running: 'badge-info' }
  return <span className={`badge ${m[s] ?? 'badge-gray'}`}>{s}</span>
}

export default function ConnectionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const connId = Number(id)
  const navigate = useNavigate()
  const [tab, setTab] = useState<Tab>('overview')

  const [connection, setConnection] = useState<Connection | null>(null)
  const [stats, setStats] = useState<ConnectionStats | null>(null)
  const [variables, setVariables] = useState<Variable[]>([])
  const [favorites, setFavorites] = useState<Favorite[]>([])
  const [diagSessions, setDiagSessions] = useState<DiagnosticSession[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [testMsg, setTestMsg] = useState('')
  const [runningDiag, setRunningDiag] = useState(false)
  const [diagType, setDiagType] = useState<SessionType>('health_check')

  const [showVarModal, setShowVarModal] = useState(false)
  const [varForm, setVarForm] = useState<VarForm>(emptyVarForm)
  const [savingVar, setSavingVar] = useState(false)
  const [editingFav, setEditingFav] = useState<FavEditState | null>(null)

  const load = async () => {
    setLoading(true)
    try {
      const [conns, st, vars, favs, sessions] = await Promise.allSettled([
        getConnections(),
        getConnectionStats(connId),
        getVariables(connId),
        getFavorites(connId),
        getDiagnosticSessions(),
      ])
      if (conns.status === 'fulfilled') {
        const c = conns.value.find(x => x.id === connId)
        if (!c) { navigate('/connections'); return }
        setConnection(c)
      }
      if (st.status === 'fulfilled') setStats(st.value)
      if (vars.status === 'fulfilled') setVariables(vars.value)
      if (favs.status === 'fulfilled') setFavorites(favs.value)
      if (sessions.status === 'fulfilled')
        setDiagSessions(sessions.value.filter(s => s.connection_id === connId))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [connId])

  const handleTest = async () => {
    try {
      const r = await testConnection(connId)
      setTestMsg(r.message || r.status)
      load()
    } catch {
      setTestMsg('Test failed')
    }
  }

  const handleAddVar = async () => {
    if (!varForm.name.trim() || !varForm.node_id.trim()) return
    setSavingVar(true)
    try {
      await createVariable({ ...varForm, connection_id: connId, is_active: true })
      setShowVarModal(false)
      setVarForm(emptyVarForm)
      load()
    } catch { setError('Failed to create variable') }
    finally { setSavingVar(false) }
  }

  const handleDeleteVar = async (varId: number) => {
    if (!confirm('Delete this variable?')) return
    try { await deleteVariable(varId); load() }
    catch { setError('Failed to delete variable') }
  }

  const handleAddFavorite = async (variable: Variable) => {
    try {
      await createFavorite({
        variable_id: variable.id, connection_id: connId,
        custom_label: variable.name, show_on_dashboard: false,
      })
      load()
    } catch { setError('Failed to add favorite') }
  }

  const handleDeleteFav = async (favId: number) => {
    if (!confirm('Remove favorite?')) return
    try { await deleteFavorite(favId); load() }
    catch { setError('Failed to remove favorite') }
  }

  const handleSaveFav = async () => {
    if (!editingFav) return
    try {
      await updateFavorite(editingFav.id, {
        custom_label: editingFav.custom_label,
        unit: editingFav.unit,
        color: editingFav.color,
        alarm_enabled: editingFav.alarm_enabled,
        alarm_min: editingFav.alarm_min !== '' ? Number(editingFav.alarm_min) : null,
        alarm_max: editingFav.alarm_max !== '' ? Number(editingFav.alarm_max) : null,
      })
      setEditingFav(null)
      load()
    } catch { setError('Failed to save favorite') }
  }

  const handleRunDiag = async () => {
    setRunningDiag(true)
    try { await runDiagnostic(connId, diagType); load() }
    catch { setError('Failed to run diagnostic') }
    finally { setRunningDiag(false) }
  }

  if (loading) return <div className="loading">Loading connection…</div>
  if (!connection) return <div className="error-msg">Connection not found</div>

  const favVarIds = new Set(favorites.map(f => f.variable_id))

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>{connection.name}</h2>
          <div className="page-subtitle" style={{ fontFamily: 'monospace' }}>{connection.url}</div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {statusBadge(connection.status)}
          <button className="btn btn-secondary btn-sm" onClick={handleTest}>🔬 Test</button>
          <button className="btn btn-ghost btn-sm" onClick={() => navigate('/connections')}>← Back</button>
        </div>
      </div>

      {error && <div className="error-msg">{error}</div>}
      {testMsg && (
        <div style={{ marginBottom: 12, padding: '8px 14px', background: 'rgba(59,130,246,0.1)', border: '1px solid rgba(59,130,246,0.3)', borderRadius: 6, fontSize: 13, color: 'var(--color-primary)' }}>
          {testMsg}
        </div>
      )}

      <div className="tabs">
        {(['overview', 'variables', 'favorites', 'diagnostics'] as Tab[]).map(t => (
          <button key={t} className={`tab-btn ${tab === t ? 'active' : ''}`} onClick={() => setTab(t)}>
            {t.charAt(0).toUpperCase() + t.slice(1)}
            {t === 'variables' && variables.length > 0 && <span style={{ marginLeft: 6, fontSize: 10, background: 'var(--color-surface-2)', borderRadius: 99, padding: '1px 6px' }}>{variables.length}</span>}
            {t === 'favorites' && favorites.length > 0 && <span style={{ marginLeft: 6, fontSize: 10, background: 'var(--color-surface-2)', borderRadius: 99, padding: '1px 6px' }}>{favorites.length}</span>}
          </button>
        ))}
      </div>

      {/* ── Overview ── */}
      {tab === 'overview' && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
          <div className="card">
            <div className="card-title">Connection Info</div>
            <div className="info-grid">
              <div className="info-label">Name</div><div className="info-value">{connection.name}</div>
              <div className="info-label">URL</div><div className="info-value">{connection.url}</div>
              <div className="info-label">Status</div><div className="info-value">{statusBadge(connection.status)}</div>
              <div className="info-label">Description</div><div className="info-value">{connection.description || '—'}</div>
              <div className="info-label">Timeout</div><div className="info-value">{connection.timeout ?? '—'} s</div>
              <div className="info-label">Retries</div><div className="info-value">{connection.retry_count ?? '—'}</div>
              <div className="info-label">Last Connected</div>
              <div className="info-value">{connection.last_connected ? new Date(connection.last_connected).toLocaleString() : '—'}</div>
              <div className="info-label">Created</div>
              <div className="info-value">{connection.created_at ? new Date(connection.created_at).toLocaleString() : '—'}</div>
            </div>
          </div>
          <div className="card">
            <div className="card-title">Statistics</div>
            {stats ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                {[
                  { label: 'Variables', value: stats.variable_count, icon: '📈' },
                  { label: 'Favorites', value: stats.favorite_count, icon: '⭐' },
                  { label: 'Diag Sessions', value: stats.diagnostic_session_count, icon: '🔍' },
                ].map(item => (
                  <div key={item.label} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '12px 16px', background: 'var(--color-surface-2)', borderRadius: 6, border: '1px solid var(--color-border)' }}>
                    <span style={{ color: 'var(--color-text-muted)' }}>{item.icon} {item.label}</span>
                    <span style={{ fontSize: 22, fontWeight: 700 }}>{item.value}</span>
                  </div>
                ))}
              </div>
            ) : <div style={{ color: 'var(--color-text-muted)' }}>No stats available</div>}
          </div>
        </div>
      )}

      {/* ── Variables ── */}
      {tab === 'variables' && (
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <div className="section-title" style={{ margin: 0 }}>Variables</div>
            <button className="btn btn-primary btn-sm" onClick={() => setShowVarModal(true)}>+ Add Variable</button>
          </div>
          {variables.length === 0 ? (
            <div className="empty-state">No variables configured for this connection.</div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Node ID</th>
                    <th>Type</th>
                    <th>Value</th>
                    <th>Updated</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {variables.map(v => (
                    <tr key={v.id}>
                      <td style={{ fontWeight: 500 }}>{v.name}</td>
                      <td style={{ fontFamily: 'monospace', fontSize: 12, color: 'var(--color-text-muted)' }}>{v.node_id}</td>
                      <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>{v.data_type || '—'}</td>
                      <td style={{ fontFamily: 'monospace' }}>{v.value !== undefined && v.value !== null ? String(v.value) : '—'}</td>
                      <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>
                        {v.last_updated ? new Date(v.last_updated).toLocaleString() : '—'}
                      </td>
                      <td>
                        <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                          {favVarIds.has(v.id) ? (
                            <span className="badge badge-warning">⭐ Fav</span>
                          ) : (
                            <button className="btn btn-ghost btn-sm" title="Add to favorites" onClick={() => handleAddFavorite(v)}>☆ Fav</button>
                          )}
                          <button className="btn btn-danger btn-sm" onClick={() => handleDeleteVar(v.id)}>🗑</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {showVarModal && (
            <div className="modal-overlay" onClick={() => setShowVarModal(false)}>
              <div className="modal" onClick={e => e.stopPropagation()}>
                <div className="modal-header">
                  <h3>Add Variable</h3>
                  <button className="modal-close" onClick={() => setShowVarModal(false)}>✕</button>
                </div>
                <div className="form-group">
                  <label className="form-label">Variable Name *</label>
                  <input className="form-control" placeholder="e.g. Temperature Sensor 1"
                    value={varForm.name} onChange={e => setVarForm(p => ({ ...p, name: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Node ID *</label>
                  <input className="form-control" placeholder='e.g. ns=2;s=Channel1.Device1.Tag1'
                    value={varForm.node_id} onChange={e => setVarForm(p => ({ ...p, node_id: e.target.value }))} />
                </div>
                <div className="form-row">
                  <div className="form-group">
                    <label className="form-label">Data Type</label>
                    <select className="form-control" value={varForm.data_type} onChange={e => setVarForm(p => ({ ...p, data_type: e.target.value }))}>
                      {['Float', 'Double', 'Int16', 'Int32', 'Int64', 'Boolean', 'String', 'DateTime'].map(t =>
                        <option key={t} value={t}>{t}</option>
                      )}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Description</label>
                    <input className="form-control" placeholder="Optional"
                      value={varForm.description} onChange={e => setVarForm(p => ({ ...p, description: e.target.value }))} />
                  </div>
                </div>
                <div className="form-actions">
                  <button className="btn btn-secondary" onClick={() => setShowVarModal(false)}>Cancel</button>
                  <button className="btn btn-primary" onClick={handleAddVar} disabled={savingVar}>
                    {savingVar ? 'Saving…' : 'Add Variable'}
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── Favorites ── */}
      {tab === 'favorites' && (
        <div className="card">
          <div className="section-title">Favorites</div>
          {favorites.length === 0 ? (
            <div className="empty-state">No favorites yet. Star a variable in the Variables tab.</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              {favorites.map(fav => (
                <div key={fav.id} style={{ padding: '14px 16px', background: 'var(--color-surface-2)', borderRadius: 6, border: `1px solid ${fav.color || 'var(--color-border)'}` }}>
                  {editingFav?.id === fav.id ? (
                    <div>
                      <div className="form-row" style={{ marginBottom: 10 }}>
                        <div className="form-group" style={{ margin: 0 }}>
                          <label className="form-label">Label</label>
                          <input className="form-control" value={editingFav.custom_label} onChange={e => setEditingFav(p => p && ({ ...p, custom_label: e.target.value }))} />
                        </div>
                        <div className="form-group" style={{ margin: 0 }}>
                          <label className="form-label">Unit</label>
                          <input className="form-control" value={editingFav.unit} onChange={e => setEditingFav(p => p && ({ ...p, unit: e.target.value }))} />
                        </div>
                      </div>
                      <div className="form-row" style={{ marginBottom: 10 }}>
                        <div className="form-group" style={{ margin: 0 }}>
                          <label className="form-label">Alarm Min</label>
                          <input className="form-control" type="number" value={editingFav.alarm_min} onChange={e => setEditingFav(p => p && ({ ...p, alarm_min: e.target.value }))} />
                        </div>
                        <div className="form-group" style={{ margin: 0 }}>
                          <label className="form-label">Alarm Max</label>
                          <input className="form-control" type="number" value={editingFav.alarm_max} onChange={e => setEditingFav(p => p && ({ ...p, alarm_max: e.target.value }))} />
                        </div>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 10 }}>
                        <input type="checkbox" id={`alarm-${fav.id}`} checked={editingFav.alarm_enabled}
                          onChange={e => setEditingFav(p => p && ({ ...p, alarm_enabled: e.target.checked }))} />
                        <label htmlFor={`alarm-${fav.id}`} style={{ fontSize: 13, color: 'var(--color-text)' }}>Alarm enabled</label>
                        <input type="color" value={editingFav.color || '#334155'}
                          onChange={e => setEditingFav(p => p && ({ ...p, color: e.target.value }))}
                          style={{ marginLeft: 'auto', width: 32, height: 28, background: 'none', border: '1px solid var(--color-border)', borderRadius: 4, cursor: 'pointer' }} />
                      </div>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <button className="btn btn-primary btn-sm" onClick={handleSaveFav}>Save</button>
                        <button className="btn btn-secondary btn-sm" onClick={() => setEditingFav(null)}>Cancel</button>
                      </div>
                    </div>
                  ) : (
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                      <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                          <span className="alarm-indicator" style={{ background: fav.is_alarm_active ? 'var(--color-danger)' : 'var(--color-success)' }} />
                          <span style={{ fontWeight: 600 }}>{fav.custom_label || fav.variable_name || `Fav #${fav.id}`}</span>
                          {fav.unit && <span style={{ color: 'var(--color-text-muted)', fontSize: 11 }}>{fav.unit}</span>}
                          {fav.is_alarm_active && <span className="badge badge-danger" style={{ fontSize: 10 }}>ALARM</span>}
                        </div>
                        <div style={{ marginTop: 4, fontSize: 12, color: 'var(--color-text-muted)' }}>
                          Value: <strong style={{ color: 'var(--color-text)', fontFamily: 'monospace' }}>
                            {fav.current_value !== undefined && fav.current_value !== null ? String(fav.current_value) : '—'}
                          </strong>
                          {fav.alarm_enabled && (fav.alarm_min !== null || fav.alarm_max !== null) && (
                            <span style={{ marginLeft: 12 }}>
                              Limits: [{fav.alarm_min ?? '—'} … {fav.alarm_max ?? '—'}]
                            </span>
                          )}
                        </div>
                      </div>
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button className="btn btn-ghost btn-sm" onClick={() => setEditingFav({
                          id: fav.id,
                          custom_label: fav.custom_label || fav.variable_name || '',
                          unit: fav.unit || '',
                          color: fav.color || '',
                          alarm_min: fav.alarm_min != null ? String(fav.alarm_min) : '',
                          alarm_max: fav.alarm_max != null ? String(fav.alarm_max) : '',
                          alarm_enabled: fav.alarm_enabled ?? false,
                        })}>✏️ Edit</button>
                        <button className="btn btn-danger btn-sm" onClick={() => handleDeleteFav(fav.id)}>🗑</button>
                      </div>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ── Diagnostics ── */}
      {tab === 'diagnostics' && (
        <div>
          <div className="card" style={{ marginBottom: 20 }}>
            <div className="section-title">Run Diagnostic</div>
            <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
              <select className="form-control" style={{ maxWidth: 220 }} value={diagType}
                onChange={e => setDiagType(e.target.value as SessionType)}>
                <option value="health_check">Health Check</option>
                <option value="connection_test">Connection Test</option>
                <option value="full_scan">Full Scan</option>
              </select>
              <button className="btn btn-primary" onClick={handleRunDiag} disabled={runningDiag}>
                {runningDiag ? '⏳ Running…' : '▶ Run Diagnostic'}
              </button>
            </div>
          </div>

          <div className="card">
            <div className="section-title">Sessions ({diagSessions.length})</div>
            {diagSessions.length === 0 ? (
              <div className="empty-state">No diagnostic sessions yet.</div>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Status</th>
                      <th>Result</th>
                      <th>Duration</th>
                      <th>Started</th>
                    </tr>
                  </thead>
                  <tbody>
                    {diagSessions.map(s => (
                      <tr key={s.id}>
                        <td style={{ fontFamily: 'monospace', fontSize: 12 }}>{s.session_type}</td>
                        <td>{diagStatusBadge(s.status)}</td>
                        <td style={{ fontSize: 12, color: 'var(--color-text-muted)', maxWidth: 220 }} className="truncate">{s.result || '—'}</td>
                        <td style={{ fontSize: 12, fontFamily: 'monospace' }}>{s.duration_ms != null ? `${s.duration_ms}ms` : '—'}</td>
                        <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{new Date(s.started_at).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
