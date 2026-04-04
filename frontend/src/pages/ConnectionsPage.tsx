import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  getConnections, createConnection, deleteConnection, testConnection,
  type Connection,
} from '../api/client'

interface FormState {
  name: string
  url: string
  description: string
  timeout: string
  retry_count: string
}

const emptyForm: FormState = { name: '', url: '', description: '', timeout: '30', retry_count: '3' }

function statusBadge(status: string) {
  const map: Record<string, string> = {
    connected: 'badge-success', disconnected: 'badge-gray',
    error: 'badge-danger', connecting: 'badge-warning',
  }
  const dotMap: Record<string, string> = {
    connected: 'dot-success', disconnected: 'dot-gray',
    error: 'dot-danger', connecting: 'dot-warning',
  }
  return (
    <span className={`badge ${map[status] ?? 'badge-gray'}`}>
      <span className={`dot ${dotMap[status] ?? 'dot-gray'}`} />
      {status}
    </span>
  )
}

export default function ConnectionsPage() {
  const [connections, setConnections] = useState<Connection[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showModal, setShowModal] = useState(false)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [testingId, setTestingId] = useState<number | null>(null)
  const [testResults, setTestResults] = useState<Record<number, string>>({})
  const navigate = useNavigate()

  const load = () => {
    setLoading(true)
    getConnections()
      .then(setConnections)
      .catch(() => setError('Failed to load connections'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleCreate = async () => {
    if (!form.name.trim() || !form.url.trim()) return
    setSaving(true)
    try {
      await createConnection({
        name: form.name, url: form.url, description: form.description,
        timeout: Number(form.timeout) || 30,
        retry_count: Number(form.retry_count) || 3,
      })
      setShowModal(false)
      setForm(emptyForm)
      load()
    } catch {
      setError('Failed to create connection')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Delete this connection?')) return
    try {
      await deleteConnection(id)
      load()
    } catch {
      setError('Failed to delete connection')
    }
  }

  const handleTest = async (id: number) => {
    setTestingId(id)
    try {
      const res = await testConnection(id)
      setTestResults(prev => ({ ...prev, [id]: res.message || res.status }))
      load()
    } catch {
      setTestResults(prev => ({ ...prev, [id]: 'Test failed' }))
    } finally {
      setTestingId(null)
    }
  }

  if (loading) return <div className="loading">Loading connections…</div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>OPC UA Connections</h2>
          <div className="page-subtitle">Manage server connections</div>
        </div>
        <button className="btn btn-primary" onClick={() => setShowModal(true)}>
          + Add Connection
        </button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      <div className="card">
        {connections.length === 0 ? (
          <div className="empty-state">No connections yet. Add your first OPC UA connection.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>URL</th>
                  <th>Status</th>
                  <th>Last Connected</th>
                  <th>Test Result</th>
                  <th style={{ textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {connections.map(conn => (
                  <tr key={conn.id}>
                    <td style={{ fontWeight: 600 }}>{conn.name}</td>
                    <td style={{ fontFamily: 'monospace', fontSize: 12, color: 'var(--color-text-muted)' }}>{conn.url}</td>
                    <td>{statusBadge(conn.status)}</td>
                    <td style={{ color: 'var(--color-text-muted)', fontSize: 12 }}>
                      {conn.last_connected ? new Date(conn.last_connected).toLocaleString() : '—'}
                    </td>
                    <td style={{ fontSize: 12 }}>
                      {testResults[conn.id]
                        ? <span style={{ color: testResults[conn.id].toLowerCase().includes('fail') ? 'var(--color-danger)' : 'var(--color-success)' }}>
                            {testResults[conn.id]}
                          </span>
                        : <span style={{ color: 'var(--color-text-muted)' }}>—</span>
                      }
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                        <button
                          className="btn btn-ghost btn-sm"
                          onClick={() => handleTest(conn.id)}
                          disabled={testingId === conn.id}
                        >
                          {testingId === conn.id ? '…' : '🔬 Test'}
                        </button>
                        <button
                          className="btn btn-secondary btn-sm"
                          onClick={() => navigate(`/connections/${conn.id}`)}
                        >
                          Details →
                        </button>
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => handleDelete(conn.id)}
                        >
                          🗑
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Add OPC UA Connection</h3>
              <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
            </div>
            <div className="form-group">
              <label className="form-label">Name *</label>
              <input className="form-control" placeholder="e.g. Production Server"
                value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} />
            </div>
            <div className="form-group">
              <label className="form-label">OPC UA URL *</label>
              <input className="form-control" placeholder="opc.tcp://192.168.1.100:4840"
                value={form.url} onChange={e => setForm(p => ({ ...p, url: e.target.value }))} />
            </div>
            <div className="form-group">
              <label className="form-label">Description</label>
              <input className="form-control" placeholder="Optional description"
                value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} />
            </div>
            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Timeout (s)</label>
                <input className="form-control" type="number" min={1}
                  value={form.timeout} onChange={e => setForm(p => ({ ...p, timeout: e.target.value }))} />
              </div>
              <div className="form-group">
                <label className="form-label">Retry Count</label>
                <input className="form-control" type="number" min={0}
                  value={form.retry_count} onChange={e => setForm(p => ({ ...p, retry_count: e.target.value }))} />
              </div>
            </div>
            <div className="form-actions">
              <button className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleCreate} disabled={saving}>
                {saving ? 'Saving…' : 'Create Connection'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
