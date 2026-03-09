export interface Disruption {
  id: number;
  disruption_number: string;
  description: string;
  line_id: number;
  sub_system_id: number;
  category_id?: number;
  duration_minutes?: number;
  service_required: boolean;
  start_datetime: string;
  end_datetime?: string;
  notes?: string;
  archived: boolean;
  created_at: string;
  updated_at: string;
}

export interface DisruptionCreate {
  description: string;
  line_id: number;
  sub_system_id: number;
  category_id?: number;
  duration_minutes?: number;
  service_required: boolean;
  start_datetime: string;
  end_datetime?: string;
  notes?: string;
}

export interface Line {
  id: number;
  name: string;
  description?: string;
  created_at: string;
  updated_at: string;
}

export interface LineCreate {
  name: string;
  description?: string;
}

export interface SubSystem {
  id: number;
  line_id: number;
  name: string;
  description?: string;
  created_at: string;
  updated_at: string;
}

export interface SubSystemCreate {
  line_id: number;
  name: string;
  description?: string;
}

export interface KPI {
  total_disruptions: number;
  disruptions_this_month: number;
  total_duration_hours: number;
  average_duration_hours: number;
  with_service_required: number;
  service_percentage: number;
  trend: 'increasing' | 'decreasing' | 'stable';
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  per_page: number;
  pages: number;
}
