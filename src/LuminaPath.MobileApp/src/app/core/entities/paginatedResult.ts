export class PaginateResult<T> {
    data: Array<T>
    pageIndex: number;
    totalPages: number;

    constructor() {
        this.data = []
        this.pageIndex = 1
        this.totalPages = 1
    }
}
