enum StatusOptions {
  Any,
  Completed,
  Open,
}

export class MediaFilter {
  SearchString: string;
  Status: StatusOptions;
  Paging: Paging;

  constructor() {
    this.SearchString = "";
    this.Status = StatusOptions.Any;
    this.Paging = new Paging();
  }

  setCount(count: number){
    this.Paging.Count = count;
    this.Paging.PageIndex = 1;
  }

  setPageIndex(pageIndex: number) {
    this.Paging.PageIndex = pageIndex;
    this.Paging.Count = 0;
  }
}

export class Paging {
  PageIndex: number;
  Count: number;

  constructor() {
    this.PageIndex = 1;
    this.Count = 0;
  }
}
