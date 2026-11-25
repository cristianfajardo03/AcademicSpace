import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { RequireAuth } from './components/RequireAuth';
import { ApprovalsPage } from './pages/ApprovalsPage';
import { AvailabilityPage } from './pages/AvailabilityPage';
import { HistoryPage } from './pages/HistoryPage';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { MyReservationsPage } from './pages/MyReservationsPage';
import { ReservationsPage } from './pages/ReservationsPage';
import { SpacesPage } from './pages/SpacesPage';
import { UsersPage } from './pages/UsersPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/"
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route index element={<HomePage />} />
        <Route
          path="usuarios"
          element={
            <RequireAuth roles={['Administrador']}>
              <UsersPage />
            </RequireAuth>
          }
        />
        <Route
          path="espacios"
          element={
            <RequireAuth roles={['Administrador']}>
              <SpacesPage />
            </RequireAuth>
          }
        />
        <Route path="disponibilidad" element={<AvailabilityPage />} />
        <Route
          path="mis-solicitudes"
          element={
            <RequireAuth roles={['Solicitante']}>
              <MyReservationsPage />
            </RequireAuth>
          }
        />
        <Route
          path="pendientes"
          element={
            <RequireAuth roles={['Aprobador']}>
              <ApprovalsPage />
            </RequireAuth>
          }
        />
        <Route
          path="reservas"
          element={
            <RequireAuth roles={['Administrador', 'Aprobador']}>
              <ReservationsPage />
            </RequireAuth>
          }
        />
        <Route path="historial" element={<HistoryPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
