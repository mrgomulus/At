import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: { 'Content-Type': 'application/json' },
})

// ── Types ──────────────────────────────────────────────────

export interface Connection {
  id: number
  name: string
  url: string
  status: 'connected' | 'disconnected' | 'error' | 'connecting'
  last_connected?: string
  created_at?: string
  description?: string
  timeout?: number
  retry_count?: number
}

export interface Variable {
  id: number
  connection_id: number
  node_id: string
  name: string
  data_type?: string
  value?: string | number | boolean | null
  last_updated?: string
  is_active: boolean
  description?: string
}

export interface Favorite {
  id: number
  variable_id: number
  connection_id: number
  custom_label?: string
  variable_name?: string
  current_value?: string | number | boolean | null
  unit?: string
  color?: string
  alarm_enabled?: boolean
  alarm_min?: number | null
  alarm_max?: number | null
  show_on_dashboard: boolean
  is_alarm_active?: boolean
  last_updated?: string
}

export interface DiagnosticSession {
  id: number
  connection_id: number
  connection_name?: string
  session_type: string
  status: 'running' | 'passed' | 'failed' | 'warning'
  result?: string
  duration_ms?: number
  started_at: string
  finished_at?: string
  details?: Record<string, unknown>
}

export interface DiagnosticSummary {
  total_sessions: number
  passed: number
  failed: number
  warnings: number
  pass_rate: number
}

export interface Disruption {
  id: number
  disruption_number?: string
  description: string
  severity: 'low' | 'medium' | 'high' | 'critical'
  line?: string
  category?: string
  start_datetime: string
  end_datetime?: string
  duration_minutes?: number
  service_required: boolean
  notes?: string
  archived: boolean
  created_at?: string
}

export interface KpiData {
  total_disruptions?: number
  disruptions_this_month?: number
  connected_opc_ua?: number
  total_variables?: number
  [key: string]: unknown
}

export interface ConnectionStats {
  variable_count: number
  favorite_count: number
  diagnostic_session_count: number
}

// ── Connections ────────────────────────────────────────────

export const getConnections = () =>
  api.get<Connection[]>('/connections/').then(r => r.data)

export const createConnection = (data: Partial<Connection>) =>
  api.post<Connection>('/connections/', data).then(r => r.data)

export const updateConnection = (id: number, data: Partial<Connection>) =>
  api.put<Connection>(`/connections/${id}`, data).then(r => r.data)

export const deleteConnection = (id: number) =>
  api.delete(`/connections/${id}`)

export const testConnection = (id: number) =>
  api.post<{ status: string; message: string }>(`/connections/${id}/test`).then(r => r.data)

export const getConnectionStats = (id: number) =>
  api.get<ConnectionStats>(`/connections/${id}/stats`).then(r => r.data)

// ── Variables ──────────────────────────────────────────────

export const getVariables = (connectionId: number) =>
  api.get<Variable[]>(`/variables/?connection_id=${connectionId}`).then(r => r.data)

export const createVariable = (data: Partial<Variable>) =>
  api.post<Variable>('/variables/', data).then(r => r.data)

export const updateVariable = (id: number, data: Partial<Variable>) =>
  api.put<Variable>(`/variables/${id}`, data).then(r => r.data)

export const deleteVariable = (id: number) =>
  api.delete(`/variables/${id}`)

export const recordVariable = (id: number) =>
  api.post(`/variables/${id}/record`).then(r => r.data)

export const getVariableHistory = (id: number) =>
  api.get(`/variables/${id}/history`).then(r => r.data)

// ── Favorites ──────────────────────────────────────────────

export const getFavorites = (connectionId: number) =>
  api.get<Favorite[]>(`/favorites/?connection_id=${connectionId}`).then(r => r.data)

export const getDashboardFavorites = () =>
  api.get<Favorite[]>('/favorites/dashboard/all').then(r => r.data)

export const createFavorite = (data: Partial<Favorite>) =>
  api.post<Favorite>('/favorites/', data).then(r => r.data)

export const updateFavorite = (id: number, data: Partial<Favorite>) =>
  api.put<Favorite>(`/favorites/${id}`, data).then(r => r.data)

export const deleteFavorite = (id: number) =>
  api.delete(`/favorites/${id}`)

// ── Diagnostics ────────────────────────────────────────────

export const runDiagnostic = (connId: number, sessionType = 'health_check') =>
  api.post<DiagnosticSession>(`/diagnostics/run/${connId}?session_type=${sessionType}`).then(r => r.data)

export const getDiagnosticSessions = () =>
  api.get<DiagnosticSession[]>('/diagnostics/sessions').then(r => r.data)

export const getDiagnosticSummary = () =>
  api.get<DiagnosticSummary>('/diagnostics/summary').then(r => r.data)

// ── Disruptions ────────────────────────────────────────────

export const getDisruptions = () =>
  api.get<Disruption[]>('/disruptions/').then(r => r.data)

export const createDisruption = (data: Partial<Disruption>) =>
  api.post<Disruption>('/disruptions/', data).then(r => r.data)

export const updateDisruption = (id: number, data: Partial<Disruption>) =>
  api.put<Disruption>(`/disruptions/${id}`, data).then(r => r.data)

export const deleteDisruption = (id: number) =>
  api.delete(`/disruptions/${id}`)

export const getDisruptionLines = () =>
  api.get<string[]>('/disruptions/lines/all').then(r => r.data)

export const getDisruptionCategories = () =>
  api.get<string[]>('/disruptions/categories/all').then(r => r.data)

// ── KPIs ───────────────────────────────────────────────────

export const getKpis = () =>
  api.get<KpiData>('/kpis/').then(r => r.data)

export const getKpiHealth = () =>
  api.get('/kpis/health').then(r => r.data)

export default api
