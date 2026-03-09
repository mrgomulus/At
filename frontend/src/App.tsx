import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import DashboardPage from './pages/DashboardPage';
import DisruptionsPage from './pages/DisruptionsPage';
import SettingsPage from './pages/SettingsPage';

const App: React.FC = () => {
  return (
    <Router>
      <div className="app">
        <nav className="navbar">
          <div className="navbar-content">
            <h1>Disruption Management System</h1>
            <nav>
              <Link to="/">Dashboard</Link>
              <Link to="/disruptions">Disruptions</Link>
              <Link to="/settings">Settings</Link>
            </nav>
          </div>
        </nav>
        
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/disruptions" element={<DisruptionsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </div>
    </Router>
  );
};

export default App;
