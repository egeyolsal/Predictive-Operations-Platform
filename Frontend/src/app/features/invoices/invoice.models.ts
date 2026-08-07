export enum InvoiceType {
  Inbound = 0,             // Giriş / Purchase
  Outbound = 1,            // Çıkış / Sale
  InternalConsumption = 2  // İç Tüketim
}

export interface InvoiceLineItemResponseDto {
  id: number;
  inventoryItemId: number;
  inventoryItemName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface InvoiceResponseDto {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  type: string;
  customerId?: number;
  customerName?: string;
  totalAmount: number;
  isCancelled: boolean;
  lineItems: InvoiceLineItemResponseDto[];
}

export interface InvoiceLineItemCreateDto {
  inventoryItemId: number;
  quantity: number;
  unitPrice: number;
}

export interface InvoiceCreateDto {
  invoiceNumber: string;
  invoiceDate: string; // ISO string
  type: InvoiceType;
  customerId?: number | null;
  lineItems: InvoiceLineItemCreateDto[];
}
