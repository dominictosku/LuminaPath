import { Game } from "~/utils/games";

export class PaginateResult<T>{
    data: Array<T>
    currentPage: number;
    pages: number;

    constructor(){
        this.data = []
        this.currentPage = 1
        this.pages = 1
    }
}