import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss'],
  standalone: true,
})
export class PaginationComponent implements OnInit {

  constructor() { }

  ngOnInit() { }

  PageIndex = 1;
  totalPages = 10;
  async getPaginatedMedia(page: number) {
    if (page < 1) {
      page = 1;
    }
    if (page > this.totalPages) {
      page = this.totalPages;
    }
  }
}
