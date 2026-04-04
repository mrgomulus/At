import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import DashboardPage from './pages/DashboardPage'
import ConnectionsPage from './pages/ConnectionsPage'
import ConnectionDetailPage from './pages/ConnectionDetailPage'
import DiagnosticsPage from './pages/DiagnosticsPage'
import DisruptionsPage from './pages/DisruptionsPage'
import SettingsPage from './pages/SettingsPage'

const NAV = [
  { to: '/', icon: '📊', label: 'Dashboard', end: true },
  { to: '/connections', icon: '🔌', label: 'Connections', end: false },
  { to: '/diagnostics', icon: '🔍', label: 'Diagnostics', end: false },
  { to: '/disruptions', icon: '⚠️', label: 'Disruptions', end: false },
  { to: '/settings', icon: '⚙️', label: 'Settings', end: false },
]

export default function App() {
  return (
    <BrowserRouter>
      <div className="app-layout">
        <aside className="sidebar">
          <div className="sidebar-logo">
            <h1>⚡ OPC UA Monitor</h1>
            <span>Disruption Management</span>
          </div>
          <nav className="sidebar-nav">
            {NAV.map(({ to, icon, label, end }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                className={({ isActive }) => isActive ? 'active' : ''}
              >
                <span className="nav-icon">{icon}</span>
                {label}
              </NavLink>
            ))}
          </nav>
          <div style={{ padding: '16px', borderTop: '1px solid var(--color-border)', fontSize: '11px', color: 'var(--color-text-muted)' }}>
            v2.0.0 · Industrial Monitor
          </div>
        </aside>
        <main className="main-content">
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/connections" element={<ConnectionsPage />} />
            <Route path="/connections/:id" element={<ConnectionDetailPage />} />
            <Route path="/diagnostics" element={<DiagnosticsPage />} />
            <Route path="/disruptions" element={<DisruptionsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}
