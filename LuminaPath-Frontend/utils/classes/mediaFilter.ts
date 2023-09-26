enum StatusOptions {
  Any,
  Completed,
  Open,
}

class MediaFilter {
  SearchString?: string;
  PageIndex: number;
  Status: StatusOptions;

  constructor() {
    this.SearchString = "";
    this.PageIndex = 1;
    this.Status = StatusOptions.Any;
  }
}
