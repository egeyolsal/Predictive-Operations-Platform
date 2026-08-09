import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config/api-config';

export interface AiRequestDto {
  question: string;
}

export interface AiResponseDto {
  answer: string;
}

@Injectable({
  providedIn: 'root'
})
export class AiAssistantService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${API_BASE_URL}/AiAssistant`;

  askQuestion(question: string): Observable<AiResponseDto> {
    return this.http.post<AiResponseDto>(`${this.apiUrl}/ask`, { question });
  }
}
