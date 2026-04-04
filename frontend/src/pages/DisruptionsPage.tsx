import { useEffect, useState } from 'react'
import {
  getDisruptions, createDisruption, updateDisruption, deleteDisruption,
  getDisruptionLines, getDisruptionCategories,
  type Disruption,
} from '../api/client'

type Severity = 'low' | 'medium' | 'high' | 'critical'

interface FormState {
  description: string
  severity: Severity
  line: string
  category: string
  start_datetime: string
  end_datetime: string
  duration_minutes: string
  service_required: boolean
  notes: string
}

const emptyForm: FormState = {
  description: '', severity: 'medium', line: '', category: '',
  start_datetime: '', end_datetime: '', duration_minutes: '',
  service_required: false, notes: '',
}

function severityBadge(s: string) {
  const m: Record<string, string> = { low: 'badge-info', medium: 'badge-warning', high: 'badge-danger', critical: 'badge-danger' }
  return <span className={`badge ${m[s] ?? 'badge-gray'}`}>{s}</span>
}

function toLocalDatetime(iso?: string) {
  if (!iso) return ''
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth()+1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export default function DisruptionsPage() {
  const [disruptions, setDisruptions] = useState<Disruption[]>([])
  const [lines, setLines] = useState<string[]>([])
  const [categories, setCategories] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [filterSeverity, setFilterSeverity] = useState('')
  const [showArchived, setShowArchived] = useState(false)

  const [showModal, setShowModal] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [saving, setSaving] = useState(false)

  const load = () => {
    setLoading(true)
    Promise.allSettled([
      getDisruptions(),
      getDisruptionLines(),
      getDisruptionCategories(),
    ]).then(([dispRes, linesRes, catsRes]) => {
      if (dispRes.status === 'fulfilled') setDisruptions(dispRes.value)
      if (linesRes.status === 'fulfilled') setLines(linesRes.value)
      if (catsRes.status === 'fulfilled') setCategories(catsRes.value)
      setLoading(false)
    }).catch(() => { setError('Failed to load disruptions'); setLoading(false) })
  }

  useEffect(() => { load() }, [])

  const openNew = () => { setEditId(null); setForm(emptyForm); setShowModal(true) }
  const openEdit = (d: Disruption) => {
    setEditId(d.id)
    setForm({
      description: d.description, severity: d.severity, line: d.line || '',
      category: d.category || '',
      start_datetime: toLocalDatetime(d.start_datetime),
      end_datetime: toLocalDatetime(d.end_datetime),
      duration_minutes: d.duration_minutes != null ? String(d.duration_minutes) : '',
      service_required: d.service_required, notes: d.notes || '',
    })
    setShowModal(true)
  }

  const handleSave = async () => {
    if (!form.description.trim() || !form.start_datetime) return
    setSaving(true)
    const payload: Partial<Disruption> = {
      description: form.description, severity: form.severity,
      line: form.line || undefined, category: form.category || undefined,
      start_datetime: form.start_datetime,
      end_datetime: form.end_datetime || undefined,
      duration_minutes: form.duration_minutes ? Number(form.duration_minutes) : undefined,
      service_required: form.service_required,
      notes: form.notes || undefined,
      archived: false,
    }
    try {
      if (editId != null) await updateDisruption(editId, payload)
      else await createDisruption(payload)
      setShowModal(false)
      load()
    } catch { setError('Failed to save disruption') }
    finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    if (!confirm('Delete this disruption?')) return
    try { await deleteDisruption(id); load() }
    catch { setError('Failed to delete disruption') }
  }

  const handleArchive = async (d: Disruption) => {
    try { await updateDisruption(d.id, { archived: !d.archived }); load() }
    catch { setError('Failed to update disruption') }
  }

  if (loading) return <div className="loading">Loading disruptions…</div>

  const filtered = disruptions.filter(d => {
    if (!showArchived && d.archived) return false
    if (filterSeverity && d.severity !== filterSeverity) return false
    return true
  })

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Disruptions</h2>
          <div className="page-subtitle">Incident management and tracking</div>
        </div>
        <button className="btn btn-primary" onClick={openNew}>+ New Disruption</button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      <div className="filter-bar">
        <select className="form-control" style={{ maxWidth: 180 }} value={filterSeverity}
          onChange={e => setFilterSeverity(e.target.value)}>
          <option value="">All Severities</option>
          {['low', 'medium', 'high', 'critical'].map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--color-text-muted)', cursor: 'pointer' }}>
          <input type="checkbox" checked={showArchived} onChange={e => setShowArchived(e.target.checked)} />
          Show Archived
        </label>
        <span style={{ color: 'var(--color-text-muted)', fontSize: 12, marginLeft: 'auto' }}>
          {filtered.length} of {disruptions.length} shown
        </span>
      </div>

      <div className="card">
        {filtered.length === 0 ? (
          <div className="empty-state">No disruptions match the current filter.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Description</th>
                  <th>Severity</th>
                  <th>Line</th>
                  <th>Start</th>
                  <th>Duration</th>
                  <th>Svc</th>
                  <th style={{ textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(d => (
                  <tr key={d.id} style={{ opacity: d.archived ? 0.5 : 1 }}>
                    <td style={{ fontFamily: 'monospace', fontSize: 12, color: 'var(--color-text-muted)' }}>
                      {d.disruption_number || `#${d.id}`}
                    </td>
                    <td className="truncate" style={{ maxWidth: 240 }} title={d.description}>{d.description}</td>
                    <td>{severityBadge(d.severity)}</td>
                    <td style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>{d.line || '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--color-text-muted)', whiteSpace: 'nowrap' }}>
                      {new Date(d.start_datetime).toLocaleDateString()}
                    </td>
                    <td style={{ fontSize: 12, fontFamily: 'monospace' }}>
                      {d.duration_minutes != null ? `${d.duration_minutes}m` : '—'}
                    </td>
                    <td style={{ textAlign: 'center' }}>{d.service_required ? '🔧' : ''}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end' }}>
                        <button className="btn btn-ghost btn-sm" onClick={() => openEdit(d)}>✏️</button>
                        <button className="btn btn-ghost btn-sm" onClick={() => handleArchive(d)} title={d.archived ? 'Unarchive' : 'Archive'}>
                          {d.archived ? '📤' : '📦'}
                        </button>
                        <button className="btn btn-danger btn-sm" onClick={() => handleDelete(d.id)}>🗑</button>
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
          <div className="modal" style={{ maxWidth: 600 }} onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{editId ? 'Edit Disruption' : 'New Disruption'}</h3>
              <button className="modal-close" onClick={() => setShowModal(false)}>✕</button>
            </div>

            <div className="form-group">
              <label className="form-label">Description *</label>
              <textarea className="form-control" rows={3} placeholder="Describe the disruption…"
                value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Severity *</label>
                <select className="form-control" value={form.severity} onChange={e => setForm(p => ({ ...p, severity: e.target.value as Severity }))}>
                  {['low', 'medium', 'high', 'critical'].map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Line</label>
                <input className="form-control" list="lines-list" placeholder="e.g. Line A"
                  value={form.line} onChange={e => setForm(p => ({ ...p, line: e.target.value }))} />
                <datalist id="lines-list">{lines.map(l => <option key={l} value={l} />)}</datalist>
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Category</label>
                <input className="form-control" list="cats-list" placeholder="e.g. Mechanical"
                  value={form.category} onChange={e => setForm(p => ({ ...p, category: e.target.value }))} />
                <datalist id="cats-list">{categories.map(c => <option key={c} value={c} />)}</datalist>
              </div>
              <div className="form-group">
                <label className="form-label">Duration (minutes)</label>
                <input className="form-control" type="number" min={0} placeholder="e.g. 45"
                  value={form.duration_minutes} onChange={e => setForm(p => ({ ...p, duration_minutes: e.target.value }))} />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label">Start Date/Time *</label>
                <input className="form-control" type="datetime-local"
                  value={form.start_datetime} onChange={e => setForm(p => ({ ...p, start_datetime: e.target.value }))} />
              </div>
              <div className="form-group">
                <label className="form-label">End Date/Time</label>
                <input className="form-control" type="datetime-local"
                  value={form.end_datetime} onChange={e => setForm(p => ({ ...p, end_datetime: e.target.value }))} />
              </div>
            </div>

            <div className="form-group">
              <label className="form-label">Notes</label>
              <textarea className="form-control" rows={2} placeholder="Additional notes…"
                value={form.notes} onChange={e => setForm(p => ({ ...p, notes: e.target.value }))} />
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
              <input type="checkbox" id="svc-req" checked={form.service_required}
                onChange={e => setForm(p => ({ ...p, service_required: e.target.checked }))} />
              <label htmlFor="svc-req" style={{ fontSize: 13, color: 'var(--color-text)', cursor: 'pointer' }}>
                🔧 Service Required
              </label>
            </div>

            <div className="form-actions">
              <button className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Saving…' : editId ? 'Save Changes' : 'Create Disruption'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
