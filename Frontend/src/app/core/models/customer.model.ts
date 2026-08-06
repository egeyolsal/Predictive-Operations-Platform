export interface Customer {
  id: number;
  name: string;
  email?: string;
  phone?: string;
  address?: string;
}

export interface CustomerCreateDto {
  name: string;
  email?: string;
  phone?: string;
  address?: string;
}
