import axios from 'axios';
import type { Disruption, DisruptionCreate, Line, LineCreate, SubSystem, SubSystemCreate, KPI, PaginatedResponse } from './types';

const API_BASE_URL = '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Health check
export const healthCheck = async () => {
  const response = await api.get('/health');
  return response.data;
};

// Lines
export const getLines = async (): Promise<Line[]> => {
  const response = await api.get('/lines');
  return response.data;
};

export const createLine = async (data: LineCreate): Promise<Line> => {
  const response = await api.post('/lines', data);
  return response.data;
};

export const getLine = async (id: number): Promise<Line> => {
  const response = await api.get(`/lines/${id}`);
  return response.data;
};

// SubSystems
export const getSubSystems = async (lineId?: number): Promise<SubSystem[]> => {
  const response = await api.get('/subsystems', {
    params: lineId ? { line_id: lineId } : undefined,
  });
  return response.data;
};

export const createSubSystem = async (data: SubSystemCreate): Promise<SubSystem> => {
  const response = await api.post('/subsystems', data);
  return response.data;
};

// Disruptions
export const getDisruptions = async (
  page = 1,
  perPage = 20,
  archived = false
): Promise<PaginatedResponse<Disruption>> => {
  const response = await api.get('/disruptions', {
    params: { page, per_page: perPage, archived },
  });
  return response.data;
};

export const getDisruption = async (id: number): Promise<Disruption> => {
  const response = await api.get(`/disruptions/${id}`);
  return response.data;
};

export const createDisruption = async (data: DisruptionCreate): Promise<Disruption> => {
  const response = await api.post('/disruptions', data);
  return response.data;
};

export const updateDisruption = async (id: number, data: Partial<DisruptionCreate>): Promise<Disruption> => {
  const response = await api.put(`/disruptions/${id}`, data);
  return response.data;
};

// KPIs
export const getKPIs = async (): Promise<KPI> => {
  const response = await api.get('/kpis');
  return response.data;
};
