import { Game } from "~/utils/games";

export class PaginateResult{
    data: Array<Game>
    currentPage: number;
    pages: number;

    constructor(){
        this.data = []
        this.currentPage = 1
        this.pages = 1
    }
}