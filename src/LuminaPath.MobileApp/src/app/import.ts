import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';

@NgModule({
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TableModule],
  exports: [CommonModule, FormsModule, ReactiveFormsModule, TableModule],
})
export class ImportsModule {}
