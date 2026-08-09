import { Component, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { marked } from 'marked';
import { AiAssistantService } from '../../../core/services/ai-assistant.service';

interface ChatMessage {
  role: 'user' | 'ai';
  content: string;
  safeContent?: SafeHtml;
  isError?: boolean;
}

@Component({
  selector: 'app-ai-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule],
  templateUrl: './ai-chat-widget.html',
  styleUrl: './ai-chat-widget.scss',
})
export class AiChatWidget implements AfterViewChecked {
  private readonly aiService = inject(AiAssistantService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly isOpen = signal(false);
  readonly isLoading = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly currentMessage = signal('');

  @ViewChild('chatScroll') private chatScrollContainer!: ElementRef;
  private autoScroll = false;

  toggleChat() {
    this.isOpen.update(v => !v);
    if (this.isOpen() && this.messages().length === 0) {
      this.messages.set([{
        role: 'ai',
        content: 'Hello! I am your platform assistant. How can I help you today?',
        safeContent: this.sanitizer.bypassSecurityTrustHtml('Hello! I am your platform assistant. How can I help you today?')
      }]);
    }
  }

  async sendMessage() {
    const text = this.currentMessage().trim();
    if (!text || this.isLoading()) return;

    // Add user message
    this.messages.update(msgs => [...msgs, { role: 'user', content: text }]);
    this.currentMessage.set('');
    this.isLoading.set(true);
    this.autoScroll = true;

    try {
      this.aiService.askQuestion(text).subscribe({
        next: async (res) => {
          const rawHtml = await marked.parse(res.answer);
          const safeHtml = this.sanitizer.bypassSecurityTrustHtml(rawHtml);
          
          this.messages.update(msgs => [...msgs, {
            role: 'ai',
            content: res.answer,
            safeContent: safeHtml
          }]);
          this.isLoading.set(false);
          this.autoScroll = true;
        },
        error: (err) => {
          this.messages.update(msgs => [...msgs, {
            role: 'ai',
            content: 'An error occurred. Please try again.',
            isError: true
          }]);
          this.isLoading.set(false);
          this.autoScroll = true;
        }
      });
    } catch (e) {
      this.isLoading.set(false);
    }
  }

  ngAfterViewChecked() {
    if (this.autoScroll && this.chatScrollContainer) {
      this.chatScrollContainer.nativeElement.scrollTop = this.chatScrollContainer.nativeElement.scrollHeight;
      this.autoScroll = false;
    }
  }
}
