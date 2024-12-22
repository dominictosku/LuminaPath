export class PaginateResult<T> {
    data: Array<T>
    PageIndex: number;
    TotalPages: number;

    constructor() {
        this.data = []
        this.PageIndex = 1
        this.TotalPages = 1
    }
}