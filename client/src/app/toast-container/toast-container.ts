import { Component } from '@angular/core';
import { Toast } from '../core/toast';

@Component({
  selector: 'app-toast-container',
  imports: [],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.css',
})
export class ToastContainer {
  constructor(protected readonly toast: Toast) {}
}
