export interface CategoryItem {
  id: number;
  name: string;
  description?: string;
}

export interface CategoryCreateRequest {
  name: string;
  description?: string;
}

export interface CategoryUpdateRequest {
  name: string;
  description?: string;
}
